using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using TEKLauncher.Controls;
using TEKLauncher.Data;
using TEKLauncher.Net;
using TEKLauncher.SteamInterop.Network.Manifest;
using TEKLauncher.Utils;
using static System.IO.File;
using static System.Security.Cryptography.Aes;
using static System.Windows.Application;
using static TEKLauncher.App;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.Data.Settings;
using static TEKLauncher.Net.HTTPClient;
using static TEKLauncher.SteamInterop.Network.ContentDownloader;
using static TEKLauncher.SteamInterop.Network.Logger;
using static TEKLauncher.SteamInterop.Network.CDN.ServersList;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.SteamInterop.Network.CDN
{
    internal class CDNClient
    {
        internal CDNClient(uint DepotID, Progress Progress, ProgressBar ProgressBar, SetStatusDelegate SetStatusMethod)
        {
            Log("Instance of CDN client created");
            Initialize(ThreadsCount = DwThreadsCount);
            BaseObjectPath = $"depot/{this.DepotID = DepotID}/chunk/";
            Hosts = new string[ThreadsCount];
            Decryptors = new Aes[ThreadsCount];
            for (int Iterator = 0; Iterator < ThreadsCount; Iterator++)
            {
                Hosts[Iterator] = NextServer();
                Aes Decryptor = Create();
                Decryptor.BlockSize = 128;
                Decryptor.KeySize = 256;
                Decryptors[Iterator] = Decryptor;
            }
            Log($"Picked {ThreadsCount} the least busy CDN servers");
            BaseDownloadPath = $@"{DownloadsDirectory}\{DepotID}";
            DepotKey = DepotKeys[DepotID];
            this.Progress = Progress;
            this.ProgressBar = ProgressBar;
            SetStatus = SetStatusMethod;
            Decompressor = new VZip(ThreadsCount);
        }
        internal CDNClient(uint DepotID, Progress Progress, ProgressBar ProgressBar, SetStatusDelegate SetStatusMethod, ulong ModID) : this(DepotID, Progress, ProgressBar, SetStatusMethod)
        {
            this.ModID = ModID;
            BaseDownloadPath += $".{ModID}";
        }
        ~CDNClient()
        {
            foreach (Aes Decryptor in Decryptors)
                Decryptor.Dispose();
        }
        private int ChunkIndex, FileIndex;
        private CancellationToken Token;
        private Exception CaughtException;
        internal bool DownloadFailed = false;
        private readonly byte[] DepotKey;
        private readonly int ThreadsCount;
        private readonly uint DepotID;
        private readonly ulong ModID;
        private readonly string BaseDownloadPath, BaseObjectPath;
        private readonly string[] Hosts;
        private readonly object ProgressLock = new object();
        private readonly Aes[] Decryptors;
        private readonly Progress Progress;
        private readonly ProgressBar ProgressBar;
        private readonly SetStatusDelegate SetStatus;
        private readonly VZip Decompressor;
        private void DownloadJob(object Args)
        {
            object[] ArgsArray = (object[])Args;
            List<FileEntry> Files = (List<FileEntry>)ArgsArray[0];
            int Offset = (int)ArgsArray[1], Total = Files.Count;
            HTTPConnection Downloader = null;
            try
            {
                Downloader = CreateConnection(Hosts[Offset]);
                for (;;)
                {
                    int CurrentChunk, CurrentFile;
                    lock (ProgressLock)
                    {
                        if (FileIndex == Total)
                            return;
                        if (Files[FileIndex].Chunks.Count == 0)
                        {
                            ChunkIndex = 0;
                            FileIndex++;
                            continue;
                        }
                        CurrentChunk = ChunkIndex++;
                        if (ChunkIndex > Files[CurrentFile = FileIndex].Chunks.Count)
                        {
                            ChunkIndex = 0;
                            if (++FileIndex == Total)
                                return;
                            continue;
                        }    
                    }
                    if (DownloadFailed || Token.IsCancellationRequested)
                        return;
                    FileEntry File = Files[CurrentFile];
                    ChunkEntry Chunk = File.Chunks[CurrentChunk];
                    using (FileStream Writer = new FileStream($@"{BaseDownloadPath}\{File.Name}", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                    {
                        long Position;
                        if (File.IsSliced)
                        {
                            Position = 0L;
                            for (int Iterator = 0; Iterator < CurrentChunk; Iterator++)
                                Position += File.Chunks[Iterator].UncompressedSize;
                            Writer.Position = Position;
                        }
                        else
                            Writer.Position = Position = Chunk.Offset;
                        byte[] Data = new byte[Chunk.UncompressedSize];
                        Writer.Read(Data, 0, Chunk.UncompressedSize);
                        bool HashMatch = ComputeAdlerHash(Data) == Chunk.Checksum;
                        if (!HashMatch)
                        {
                            Data = null;
                            string ID = BitConverter.ToString(Chunk.GID).Replace("-", string.Empty), Message = null;
                            for (int AttemptsCount = 0; AttemptsCount < 5; AttemptsCount++)
                            {
                                try { Data = Downloader.DownloadChunk($"{BaseObjectPath}{ID}", Chunk.CompressedSize); }
                                catch (ValidatorException Exception) { Message = Exception.Message; }
                                if (Token.IsCancellationRequested)
                                    return;
                                if (Data is null)
                                    continue;
                                int ErrorIndex = 0;
                                try
                                {
                                    Data = AESDecrypt(Data, DepotKey, Decryptors[Offset]);
                                    ErrorIndex++;
                                    Data = Data[0] == 'V' && Data[1] == 'Z' ? Decompressor.Decompress(Data, Offset) : Zip.Decompress(Data);
                                }
                                catch
                                {
                                    Data = null;
                                    Message = LocString(LocCode.DecryptionFailure + ErrorIndex);
                                    continue;
                                }
                                if (ComputeAdlerHash(Data) != Chunk.Checksum)
                                {
                                    Data = null;
                                    Message = LocString(LocCode.AdlerHashMismatch);
                                    continue;
                                }
                                break;
                            }
                            if (Data is null)
                            {
                                Log($"({Hosts[Offset]}) Failed to download chunk {ID} for depot {DepotID}: {Message}");
                                throw new ValidatorException(Message);
                            }
                            Writer.Position = Position;
                            Writer.Write(Data, 0, Chunk.UncompressedSize);
                        }
                        lock (ProgressLock)
                        {
                            if (HashMatch)
                                Progress.IncreaseNoETA(Chunk.CompressedSize);
                            else
                                Progress.Increase(Chunk.CompressedSize);
                        }
                    }
                }
            }
            catch (Exception Exception)
            {
                DownloadFailed = true;
                CaughtException = Exception;
            }
            finally
            {
                if (!(Downloader is null))
                    Downloader.Close();
            }
        }
        internal Exception DownloadChanges(List<FileEntry> Files, long DownloadSize, CancellationToken Token)
        {
            DownloadFailed = false;
            ChunkIndex = FileIndex = 0;
            this.Token = Token;
            Progress.Total = DownloadSize;
            Current.Dispatcher.Invoke(ProgressBar.SetDownloadMode);
            Thread[] DownloadThreads = new Thread[ThreadsCount];
            for (int Iterator = 0; Iterator < ThreadsCount; Iterator++)
            {
                Thread DownloadThread = new Thread(DownloadJob);
                DownloadThread.Start(new object[] { Files, Iterator });
                DownloadThreads[Iterator] = DownloadThread;
            }
            foreach (Thread DownloadThread in DownloadThreads)
                DownloadThread.Join();
            return CaughtException;
        }
        internal DepotManifest GetManifests(ulong LatestManifestID, out DepotManifest OldManifest)
        {
            byte[] DepotKey = DepotKeys[DepotID];
            string ItemCompound = ModID == 0UL ? $"{DepotID}-" : $"{DepotID}.{ModID}-", LatestManifestFile = $@"{ManifestsDirectory}\{ItemCompound}{LatestManifestID}.manifest", LMD = LatestManifestFile + "d";
            string[] ManifestFiles = Directory.EnumerateFiles(ManifestsDirectory).Where(File => File.Contains(ItemCompound) && (File.EndsWith(".manifest") || File.EndsWith(".manifestd")) && File != LatestManifestFile && File != LMD).OrderByDescending(File => GetCreationTime(File)).ToArray();
            for (int Iterator = 1; Iterator < ManifestFiles.Length; Iterator++)
                Delete(ManifestFiles[Iterator]);
            if (ManifestFiles.Length != 0)
            {
                if (GetFileSize(ManifestFiles[0]) < 4L)
                {
                    Delete(ManifestFiles[0]);
                    OldManifest = null;
                }
                else
                {
                    Log($"Found old manifest ({Path.GetFileName(ManifestFiles[0]).Split('.')[0]}) in the directory, reading...");
                    SetStatus(LocString(LocCode.ReadingOldManifest), YellowBrush);
                    try { OldManifest = new DepotManifest(ManifestFiles[0], DepotID); }
                    catch (ValidatorException Exception)
                    {
                        Delete(ManifestFiles[0]);
                        throw Exception;
                    }
                    Log("Finished reading old manifest");
                }
            }
            else
                OldManifest = null;
            bool LME = GetFileSize(LatestManifestFile) > 3L, LMDE = GetFileSize(LMD) > 3L;
            if (!(LME || LMDE))
            {
                Log("Latest manifest not found, downloading...");
                string CompressedManifestFile = $"{LatestManifestFile}c";
                SetStatus(LocString(LocCode.DwLatestManifest), YellowBrush);
                Current.Dispatcher.Invoke(ProgressBar.SetDownloadMode);
                if (!new Downloader(Progress).TryDownloadFile(CompressedManifestFile, $"https://{Hosts[0]}/depot/{DepotID}/manifest/{LatestManifestID}/5"))
                    throw new ValidatorException(LocString(LocCode.DwManifestFailed));
                Log("Download complete, decompressing manifest...");
                Zip.Decompress(CompressedManifestFile, LatestManifestFile);
                Delete(CompressedManifestFile);
            }
            Log("Reading latest manifest...");
            SetStatus(LocString(LocCode.ReadingLatestManifest), YellowBrush);
            DepotManifest Manifest;
            string LMPath = LMDE ? LMD : LatestManifestFile;
            try { Manifest = new DepotManifest(LMPath, DepotID); }
            catch (ValidatorException Exception)
            {
                Delete(LMPath);
                throw Exception;
            }
            Log("Finished reading latest manifest");
            return Manifest;
        }
    }
}