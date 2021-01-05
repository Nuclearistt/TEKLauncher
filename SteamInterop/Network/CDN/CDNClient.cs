using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TEKLauncher.Controls;
using TEKLauncher.Data;
using TEKLauncher.Net;
using TEKLauncher.SteamInterop.Network.Manifest;
using TEKLauncher.Utils;
using static System.TimeSpan;
using static System.IO.File;
using static System.Threading.Tasks.Task;
using static System.Windows.Application;
using static TEKLauncher.App;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.Data.Settings;
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
            BaseURLs = new string[ThreadsCount];
            for (int Iterator = 0; Iterator < ThreadsCount; Iterator++)
                BaseURLs[Iterator] = $"https://{NextServer().Host}/depot/{this.DepotID = DepotID}/";
            Log($"Picked {ThreadsCount} the least busy CDN servers");
            BaseDownloadPath = $@"{DownloadsDirectory}\{DepotID}";
            DepotKey = DepotKeys[DepotID];
            Downloader = new HttpClient(Handler) { Timeout = FromSeconds(7D) };
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
        private int ChunkIndex, FileIndex;
        private CancellationToken Token;
        internal bool DownloadFailed = false;
        private readonly byte[] DepotKey;
        private readonly int ThreadsCount;
        private readonly uint DepotID;
        private readonly ulong ModID;
        private readonly string BaseDownloadPath;
        private readonly string[] BaseURLs;
        private readonly object ProgressLock = new object();
        private readonly HttpClient Downloader;
        private readonly Progress Progress;
        private readonly ProgressBar ProgressBar;
        private readonly SetStatusDelegate SetStatus;
        private readonly VZip Decompressor;
        private static readonly HttpClientHandler Handler = new HttpClientHandler { MaxConnectionsPerServer = 20 };
        private void DownloadJob(object Args)
        {
            object[] ArgsArray = (object[])Args;
            List<FileEntry> Files = (List<FileEntry>)ArgsArray[0];
            int Offset = (int)ArgsArray[1], Total = Files.Count;
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
                    if (ComputeAdlerHash(Data) != Chunk.Checksum)
                    {
                        Data = DownloadChunk(Chunk, Offset);
                        if (Data is null)
                            return;
                        Writer.Position = Position;
                        Writer.Write(Data, 0, Chunk.UncompressedSize);
                    }
                    lock (ProgressLock)
                        Progress.Increase(Chunk.CompressedSize);
                }
            }
        }
        private byte[] DownloadChunk(ChunkEntry Chunk, int CDNIndex)
        {
            string ID = BitConverter.ToString(Chunk.GID).Replace("-", string.Empty), Message = null;
            for (int AttemptsCount = 0; AttemptsCount < 5; AttemptsCount++)
            {
                byte[] Data;
                try { Data = Downloader.GetByteArrayAsync($"{BaseURLs[CDNIndex]}chunk/{ID}").Result; }
                catch { Data = null; }
                if (Token.IsCancellationRequested)
                    return null;
                if (Data is null || Data.Length != Chunk.CompressedSize)
                    continue;
                try
                {
                    Data = AESDecrypt(Data, DepotKey);
                    Data = Data[0] == 'V' && Data[1] == 'Z' ? Decompressor.Decompress(Data, CDNIndex) : Zip.Decompress(Data);
                }
                catch { continue; }
                if (ComputeAdlerHash(Data) != Chunk.Checksum)
                    continue;
                return Data;
            }
            for (int Index = 0; Index < ThreadsCount; Index++)
            {
                byte[] Data;
                try { Data = Downloader.GetAsync($"{BaseURLs[Index]}chunk/{ID}", Token).Result.Content.ReadAsByteArrayAsync().Result; }
                catch { Data = null; }
                if (Data is null || Data.Length != Chunk.CompressedSize)
                    continue;
                try { Data = AESDecrypt(Data, DepotKey); }
                catch { Message = "Failed to decrypt chunk"; continue; }
                try { Data = Data[0] == 'V' && Data[1] == 'Z' ? Decompressor.Decompress(Data, CDNIndex) : Zip.Decompress(Data); }
                catch { Message = "Failed to decompress chunk"; continue; }
                if (ComputeAdlerHash(Data) != Chunk.Checksum)
                {
                    Message = "Encountered corrupted chunk";
                    continue;
                }
                return Data;
            }
            string LogEntry = $"Failed to download chunk {ID} for depot {DepotID}";
            if (!(Message is null))
                LogEntry += $": {Message}";
            Log(LogEntry);
            DownloadFailed = true;
            throw new ValidatorException(Message ?? LocString(LocCode.DownloadFailed));
        }
        internal void DownloadChanges(List<FileEntry> Files, long DownloadSize, CancellationToken Token)
        {
            DownloadFailed = false;
            ChunkIndex = FileIndex = 0;
            this.Token = Token;
            Progress.Total = DownloadSize;
            Current.Dispatcher.Invoke(ProgressBar.SetDownloadMode);
            Task[] DownloadTasks = new Task[ThreadsCount];
            for (int Iterator = 0; Iterator < ThreadsCount; Iterator++)
                DownloadTasks[Iterator] = Factory.StartNew(DownloadJob, new object[] { Files, Iterator });
            WaitAll(DownloadTasks);
        }
        internal DepotManifest GetManifests(ulong LatestManifestID, out DepotManifest OldManifest)
        {
            byte[] DepotKey = DepotKeys[DepotID];
            string ItemCompound = ModID == 0UL ? $"{DepotID}-" : $"{DepotID}.{ModID}-", LatestManifestFile = $@"{ManifestsDirectory}\{ItemCompound}{LatestManifestID}.manifest";
            string[] ManifestFiles = Directory.EnumerateFiles(ManifestsDirectory).Where(File => File.Contains(ItemCompound) && File.EndsWith(".manifest") && File != LatestManifestFile).OrderBy(File => GetCreationTime(File)).ToArray();
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
                    Log($"Found old manifest ({Path.GetFileName(ManifestFiles[0]).Split('-')[0]}) in the directory, reading...");
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
            if (GetFileSize(LatestManifestFile) < 4L)
            {
                Log("Latest manifest not found, downloading...");
                string CompressedManifestFile = $"{LatestManifestFile}c";
                SetStatus(LocString(LocCode.DwLatestManifest), YellowBrush);
                Current.Dispatcher.Invoke(ProgressBar.SetDownloadMode);
                if (!new Downloader(Progress).TryDownloadFile(CompressedManifestFile, $"{BaseURLs[0]}manifest/{LatestManifestID}/5"))
                    throw new ValidatorException(LocString(LocCode.DwManifestFailed));
                Log("Download complete, decompressing manifest...");
                Zip.Decompress(CompressedManifestFile, LatestManifestFile);
                Delete(CompressedManifestFile);
            }
            Log("Reading manifest...");
            SetStatus(LocString(LocCode.ReadingLatestManifest), YellowBrush);
            DepotManifest Manifest;
            try { Manifest = new DepotManifest(LatestManifestFile, DepotID); }
            catch (ValidatorException Exception)
            {
                Delete(LatestManifestFile);
                throw Exception;
            }
            Log("Finished reading and decrypting manifest");
            return Manifest;
        }
    }
}