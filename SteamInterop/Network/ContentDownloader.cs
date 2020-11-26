using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using TEKLauncher.ARK;
using TEKLauncher.Controls;
using TEKLauncher.Data;
using TEKLauncher.SteamInterop.Network.CDN;
using TEKLauncher.SteamInterop.Network.Manifest;
using static System.BitConverter;
using static System.Math;
using static System.IO.File;
using static System.Threading.WaitHandle;
using static System.Threading.Tasks.Task;
using static System.Windows.Application;
using static System.Windows.Media.Brushes;
using static TEKLauncher.App;
using static TEKLauncher.ARK.DLCManager;
using static TEKLauncher.ARK.ModManager;
using static TEKLauncher.SteamInterop.Network.Logger;
using static TEKLauncher.SteamInterop.Network.SteamClient;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.SteamInterop.Network
{
    internal class ContentDownloader
    {
        internal ContentDownloader(uint DepotID, FinishDelegate FinishMethod, SetStatusDelegate SetStatusMethod, ProgressBar ProgressBar)
        {
            BaseDownloadPath = $@"{DownloadsDirectory}\{this.DepotID = DepotID}";
            Finish = FinishMethod;
            this.ProgressBar = ProgressBar;
            Progress = Current.Dispatcher.Invoke(() => this.ProgressBar.Progress);
            SetStatus = (Text, Color) => Current.Dispatcher.Invoke(() => SetStatusMethod(Text, Color));
        }
        private bool ValidationFailed = false;
        private ulong LatestManifestID, ModID, SpacewarID;
        private string BaseDownloadPath;
        private CancellationTokenSource Cancellator;
        private CDNClient CDNClient;
        private List<string> FilesToDelete;
        internal bool IsDownloading = false, IsValidating = false;
        internal int FilesMissing, FilesOutdated, FilesUpToDate;
        internal long DownloadSize, InstallSize;
        private readonly uint DepotID;
        private readonly object ProgressLock = new object();
        private readonly FinishDelegate Finish;
        private readonly Progress Progress;
        private readonly ProgressBar ProgressBar;
        private readonly SetStatusDelegate SetStatus;
        private static readonly Dictionary<uint, bool> DepotLocks = new Dictionary<uint, bool>
        {
            [346111U] = false,
            [346114U] = false,
            [375351U] = false,
            [375354U] = false,
            [375357U] = false,
            [473851U] = false,
            [473854U] = false,
            [473857U] = false,
            [1318685U] = false
        };
        private static readonly Dictionary<uint, string> DepotNames = new Dictionary<uint, string>
        {
            [346111U] = "game",
            [346114U] = "The Center DLC",
            [375351U] = "Scorched Earth DLC",
            [375354U] = "Ragnarok DLC",
            [375357U] = "Aberration DLC",
            [473851U] = "Extinction DLC",
            [473854U] = "Valguero DLC",
            [473857U] = "Genesis DLC",
            [1318685U] = "Crystal Isles DLC"
        };
        private static readonly List<ulong> ModLocks = new List<ulong>();
        internal static readonly Dictionary<uint, byte[]> DepotKeys = new Dictionary<uint, byte[]>
        {
            [480U] = new byte[]
            {
                0x1B, 0xA1, 0x48, 0x73, 0x70, 0x57, 0xFF, 0xCC, 0xFB, 0x54, 0xE6, 0x39, 0x8F, 0x62, 0x30, 0x04,
                0xAA, 0x24, 0x1B, 0x94, 0x5B, 0x02, 0x3F, 0xAD, 0xCF, 0x0C, 0x3B, 0xF1, 0xB5, 0x20, 0x45, 0xEC
            },
            [346110U] = new byte[]
            {
                0xE1, 0x6F, 0xF7, 0xF0, 0x82, 0x25, 0xD9, 0xAE, 0x36, 0x35, 0xA9, 0x88, 0x33, 0xA1, 0xC6, 0x3A,
                0xCC, 0x58, 0xB2, 0xA1, 0x04, 0xB0, 0xB3, 0x7A, 0x96, 0x05, 0xD4, 0x94, 0x68, 0x37, 0x87, 0xBE
            },
            [346111U] = new byte[]
            {
                0x5E, 0xDB, 0x30, 0x79, 0x72, 0xCD, 0x5B, 0xF1, 0xE3, 0x08, 0x1A, 0xED, 0xC9, 0x86, 0xEF, 0x72,
                0x1D, 0xFD, 0x27, 0xCA, 0xE1, 0x6D, 0x0A, 0x97, 0x6C, 0x6B, 0x7E, 0xA6, 0xE8, 0xFF, 0x20, 0x89
            },
            [346114U] = new byte[]
            {
                0xD8, 0xD4, 0x9A, 0xB9, 0x8F, 0x0F, 0x75, 0x30, 0xC0, 0xC2, 0x92, 0x62, 0x13, 0xC7, 0xAC, 0x64,
                0x62, 0x57, 0x12, 0xA2, 0xEF, 0xBB, 0xC9, 0x6E, 0x6B, 0x3F, 0x06, 0x94, 0x37, 0x41, 0xF8, 0x06
            },
            [375351U] = new byte[]
            {
                0x80, 0x57, 0x31, 0x13, 0xA7, 0xBF, 0x29, 0x45, 0x55, 0xB4, 0xC8, 0xE4, 0xE0, 0x41, 0xC6, 0x9E,
                0x7A, 0x5A, 0x69, 0x52, 0x7A, 0x4B, 0x65, 0xD3, 0xCE, 0x6F, 0x47, 0xC2, 0x38, 0x24, 0x88, 0xA7
            },
            [375354U] = new byte[]
            {
                0xE2, 0xFA, 0x71, 0xE0, 0x18, 0x10, 0xBB, 0xD5, 0x54, 0xE9, 0x47, 0x13, 0xE8, 0x4E, 0xFB, 0x08,
                0x4E, 0x0A, 0x99, 0xFF, 0x78, 0xF2, 0x1F, 0xBA, 0x3C, 0x20, 0x14, 0x10, 0xF7, 0x4E, 0x9A, 0x19
            },
            [375357U] = new byte[]
            {
                0x9B, 0xC4, 0x5B, 0x9B, 0x59, 0xF3, 0xF3, 0x21, 0x54, 0xEE, 0x76, 0xB4, 0x7F, 0xF8, 0xB6, 0x01,
                0x5E, 0x92, 0xD5, 0xA6, 0x34, 0x2C, 0x44, 0x4B, 0xED, 0x3E, 0xB2, 0x10, 0xBC, 0xD2, 0x4B, 0x60
            },
            [473851U] = new byte[]
            {
                0x99, 0xFB, 0x59, 0x71, 0x07, 0x88, 0x29, 0x42, 0xCA, 0x94, 0xE2, 0x03, 0x82, 0x15, 0xF0, 0x3F,
                0x87, 0x07, 0xD5, 0x7B, 0x05, 0x94, 0x60, 0xEC, 0x6B, 0xDE, 0x82, 0x65, 0xBF, 0x02, 0x07, 0x33
            },
            [473854U] = new byte[]
            {
                0xDA, 0xC7, 0x1F, 0x84, 0xF6, 0xDD, 0x94, 0x0B, 0x7B, 0xFE, 0x12, 0xE5, 0xDF, 0x2B, 0xB9, 0x8E,
                0xFF, 0x8B, 0xA6, 0x22, 0xD7, 0x6C, 0x30, 0x09, 0x1C, 0x52, 0xEC, 0xFD, 0x60, 0xBB, 0xF3, 0x34
            },
            [473857U] = new byte[]
            {
                0xC9, 0xBB, 0x21, 0x64, 0x42, 0x70, 0xFD, 0x68, 0x83, 0xBC, 0x3F, 0x00, 0x7A, 0xA6, 0x6F, 0x62,
                0x5E, 0xA3, 0x28, 0x1A, 0xD2, 0x19, 0x69, 0x00, 0xDF, 0xBB, 0xDF, 0x12, 0xCE, 0xB5, 0x8C, 0xE2
            },
            [1318685U] = new byte[]
            {
                0x32, 0x3F, 0x24, 0x3A, 0x7C, 0xED, 0x4F, 0x90, 0x62, 0x8B, 0xA9, 0xDA, 0x90, 0x1C, 0xA9, 0x63,
                0x02, 0x35, 0x0C, 0x23, 0xC6, 0x25, 0x15, 0x3D, 0x21, 0x42, 0x5D, 0x93, 0xDF, 0x0A, 0x62, 0xCF
            }
        };
        internal delegate void FinishDelegate();
        internal delegate void SetStatusDelegate(string Text, SolidColorBrush Color);
        private void InstallChanges(List<FileEntry> Files)
        {
            if (Game.IsRunning && ModID == 0UL)
                throw new ValidatorException("Can't install update while game is running!");
            else
            {
                Progress.Total = Files.Count;
                Current.Dispatcher.Invoke(ProgressBar.SetNumericMode);
                string BaseLocalPath = ModID == 0UL ? Game.Path : $@"{Steam.WorkshopPath}\{SpacewarID}";
                foreach (FileEntry File in Files)
                {
                    string DestinationPath = $@"{BaseLocalPath}\{File.Name}", SourcePath = $@"{BaseDownloadPath}\{File.Name}";
                    if (File.IsSliced)
                    {
                        if (!Directory.Exists(Path.GetDirectoryName(DestinationPath)))
                            throw new ValidatorException("Installation is corrupted, validate instead");
                        using (FileStream Reader = OpenRead(SourcePath))
                        using (FileStream Writer = OpenWrite(DestinationPath))
                        {
                            Writer.SetLength(File.Size);
                            foreach (ChunkEntry Chunk in File.Chunks)
                            {
                                byte[] Buffer = new byte[Chunk.UncompressedSize];
                                Reader.Read(Buffer, 0, Chunk.UncompressedSize);
                                Writer.Position = Chunk.Offset;
                                Writer.Write(Buffer, 0, Chunk.UncompressedSize);
                            }
                        }
                        Delete(SourcePath);
                    }
                    else
                    {
                        if (FileExists(DestinationPath))
                            Delete(DestinationPath);
                        Directory.CreateDirectory(Path.GetDirectoryName(DestinationPath));
                        Move(SourcePath, DestinationPath);
                    }
                    Progress.Increase();
                }
                if (!(FilesToDelete is null))
                    foreach (string File in FilesToDelete)
                        DeletePath($@"{BaseLocalPath}\{File}");
                string ItemCompound = ModID == 0UL ? $"{DepotID}-" : $"{DepotID}.{ModID}-";
                DeletePath($@"{DownloadsDirectory}\{ItemCompound}{LatestManifestID}.vcache");
                DeleteDirectory(BaseDownloadPath);
            }
        }
        private void Preallocate(List<FileEntry> Files)
        {
            if (!Directory.Exists(BaseDownloadPath))
            {
                Directory.CreateDirectory(BaseDownloadPath);
                long DiskFreeSpace = GetFreeSpace(BaseDownloadPath);
                if (InstallSize > DiskFreeSpace)
                {
                    Directory.Delete(BaseDownloadPath);
                    throw new ValidatorException($"Not enough disk free space, {ConvertBytes(InstallSize - DiskFreeSpace)} more is required");
                }
            }
            Progress.Total = Files.Count;
            Current.Dispatcher.Invoke(ProgressBar.SetNumericMode);
            foreach (FileEntry File in Files)
            {
                string FilePath = $@"{BaseDownloadPath}\{File.Name}";
                try { Directory.CreateDirectory(Path.GetDirectoryName(FilePath)); }
                catch (ArgumentException)
                {
                    DeletePath($@"{DownloadsDirectory}\{DepotID}-{LatestManifestID}.vcache");
                    throw new ValidatorException("Manifest has been decrypted incorrectly. Try again");
                }
                using (FileStream Stream = OpenWrite(FilePath))
                    Stream.SetLength(File.IsSliced ? File.Chunks.Sum(Chunk => Chunk.UncompressedSize) : File.Size);
                Progress.Increase();
            }
            Log("Preallocation complete, proceeding to download...");
        }
        private void ValidationJob(object Args)
        {
            object[] ArgsArray = (object[])Args;
            List<FileEntry> Files = (List<FileEntry>)ArgsArray[0];
            List<FileEntry> Changes = (List<FileEntry>)ArgsArray[1];
            int Total = (int)Progress.Total;
            string BaseLocalPath = ModID == 0UL ? Game.Path : $@"{Steam.WorkshopPath}\{SpacewarID}";
            try
            {
                using (SHA1 SHA = SHA1.Create())
                    for (int Iterator = (int)ArgsArray[2]; Iterator < Total; Iterator += 4)
                    {
                        if (Cancellator.IsCancellationRequested)
                            break;
                        FileEntry File = Files[Iterator];
                        string LocalFile = $@"{BaseLocalPath}\{File.Name}";
                        if (FileExists(LocalFile))
                            using (FileStream LocalFileStream = OpenRead(LocalFile))
                            {
                                byte[] FileHash;
                                try { FileHash = SHA.ComputeHash(LocalFileStream); }
                                catch { FileHash = new byte[0]; }
                                if (FileHash.SequenceEqual(File.Checksum))
                                    lock (ProgressLock)
                                        FilesUpToDate++;
                                else
                                {
                                    FileEntry ChangeFile = new FileEntry { Checksum = File.Checksum, Size = File.Size, Name = File.Name, IsSliced = true };
                                    foreach (ChunkEntry Chunk in File.Chunks)
                                    {
                                        if (Chunk.Offset + Chunk.UncompressedSize > LocalFileStream.Length)
                                            ChangeFile.Chunks.Add(Chunk);
                                        else
                                        {
                                            byte[] Buffer = new byte[Chunk.UncompressedSize];
                                            LocalFileStream.Position = Chunk.Offset;
                                            LocalFileStream.Read(Buffer, 0, Chunk.UncompressedSize);
                                            if (ComputeAdlerHash(Buffer) != Chunk.Checksum)
                                                ChangeFile.Chunks.Add(Chunk);
                                        }
                                    }
                                    long FileDownloadSize = ChangeFile.Chunks.Sum(Chunk => Chunk.CompressedSize), FileInstallSize = ChangeFile.Chunks.Sum(Chunk => Chunk.UncompressedSize);
                                    lock (ProgressLock)
                                    {
                                        FilesOutdated++;
                                        DownloadSize += FileDownloadSize;
                                        InstallSize += FileInstallSize;
                                        Changes.Add(ChangeFile);
                                    }
                                }
                            }
                        else
                        {
                            long FileDownloadSize = File.Chunks.Sum(Chunk => Chunk.CompressedSize);
                            lock (ProgressLock)
                            {
                                FilesMissing++;
                                DownloadSize += FileDownloadSize;
                                InstallSize += File.Size;
                                Changes.Add(File);
                            }
                        }
                        lock (ProgressLock)
                            Progress.Increase();
                    }
            }
            catch (Exception Exception)
            {
                ValidationFailed = true;
                Cancellator.Cancel();
                throw Exception;
            }
        }
        private List<FileEntry> ReadValidationCache(out bool IsIncomplete)
        {
            string ItemCompound = ModID == 0UL ? $"{DepotID}-" : $"{DepotID}.{ModID}-", VCacheFile = $@"{DownloadsDirectory}\{ItemCompound}{LatestManifestID}.vcache";
            foreach (string VCache in Directory.EnumerateFiles(DownloadsDirectory).Where(File => File.Contains(ItemCompound) && File.EndsWith(".vcache") && File != VCacheFile))
                Delete(VCache);
            if (FileExists(VCacheFile))
                using (FileStream CacheReader = OpenRead(VCacheFile))
                {
                    IsIncomplete = CacheReader.ReadByte() == 1;
                    byte[] Buffer = new byte[8];
                    CacheReader.Read(Buffer, 0, 8);
                    DownloadSize = ToInt64(Buffer, 0);
                    CacheReader.Read(Buffer, 0, 8);
                    InstallSize = ToInt64(Buffer, 0);
                    CacheReader.Read(Buffer, 0, 4);
                    FilesUpToDate = ToInt32(Buffer, 0);
                    CacheReader.Read(Buffer, 0, 4);
                    FilesOutdated = ToInt32(Buffer, 0);
                    CacheReader.Read(Buffer, 0, 4);
                    FilesMissing = ToInt32(Buffer, 0);
                    List<FileEntry> Changes = new List<FileEntry>(FilesOutdated + FilesMissing);
                    for (int Iterator = FilesOutdated + FilesMissing; Iterator > 0; Iterator--)
                    {
                        FileEntry File = new FileEntry();
                        File.ReadFromFile(CacheReader);
                        Changes.Add(File);
                    }
                    return Changes;
                }
            else
            {
                IsIncomplete = false;
                return null;
            }
        }
        private List<FileEntry> ComputeChanges(DepotManifest OldManifest, DepotManifest LatestManifest)
        {
            Log("Computing changes between old and new manifest...");
            SetStatus("Computing changes", YellowBrush);
            List<FileEntry> Changes = new List<FileEntry>();
            int FilesCount = LatestManifest.Files.Count, Iterator = 0, Offset = 0, OldFilesCount = OldManifest.Files.Count;
            for (; Iterator < OldFilesCount && Iterator + Offset < FilesCount; Iterator++)
            {
                FileEntry File = LatestManifest.Files[Iterator + Offset], OldFile = OldManifest.Files[Iterator];
                int Difference = OldFile.Name.CompareTo(File.Name);
                if (Difference < 0)
                {
                    if (FilesToDelete is null)
                        FilesToDelete = new List<string>();
                    FilesToDelete.Add(OldFile.Name);
                    Offset--;
                    continue;
                }
                else if (Difference > 0)
                {
                    FilesMissing++;
                    DownloadSize += File.Chunks.Sum(Chunk => Chunk.CompressedSize);
                    InstallSize += File.Size;
                    Changes.Add(File);
                    Offset++;
                    Iterator--;
                    continue;
                }
                if (!File.Checksum.SequenceEqual(OldFile.Checksum))
                {
                    FileEntry ChangeFile = new FileEntry { Checksum = File.Checksum, Size = File.Size, Name = File.Name, IsSliced = true };
                    int ChunksCount = File.Chunks.Count, OldChunksCount = OldFile.Chunks.Count;
                    for (int Iterator1 = 0; Iterator1 < Min(ChunksCount, OldChunksCount); Iterator1++)
                    {
                        ChunkEntry Chunk = File.Chunks[Iterator1], OldChunk = OldFile.Chunks[Iterator1];
                        if (!Chunk.GID.SequenceEqual(OldChunk.GID) || Chunk.Offset != OldChunk.Offset)
                        {
                            DownloadSize += Chunk.CompressedSize;
                            InstallSize += Chunk.UncompressedSize;
                            ChangeFile.Chunks.Add(Chunk);
                        }
                    }
                    if (ChunksCount > OldChunksCount)
                        for (int Iterator1 = OldChunksCount; Iterator1 < ChunksCount; Iterator1++)
                        {
                            ChunkEntry Chunk = File.Chunks[Iterator1];
                            DownloadSize += Chunk.CompressedSize;
                            InstallSize += Chunk.UncompressedSize;
                            ChangeFile.Chunks.Add(Chunk);
                        }
                    if (ChangeFile.Chunks.Count != 0)
                    {
                        FilesOutdated++;
                        Changes.Add(ChangeFile);
                    }
                }
            }
            for (int Iterator1 = Iterator; Iterator1 < OldFilesCount; Iterator1++)
            {
                if (FilesToDelete is null)
                    FilesToDelete = new List<string>();
                FilesToDelete.Add(OldManifest.Files[Iterator1].Name);
            }
            for (int Iterator1 = Iterator + Offset; Iterator1 < FilesCount; Iterator1++)
            {
                FileEntry File = LatestManifest.Files[Iterator1];
                FilesMissing++;
                DownloadSize += File.Chunks.Sum(Chunk => Chunk.CompressedSize);
                InstallSize += File.Size;
                Changes.Add(File);
            }
            Log($"Computing changes complete: {FilesOutdated} files updated and {FilesMissing} new files");
            return Changes;
        }
        private List<FileEntry> Validate(DepotManifest Manifest, List<FileEntry> Changes = null, int StartOffset = 0)
        {
            string ItemCompound = ModID == 0UL ? $"{DepotID}-" : $"{DepotID}.{ModID}-", ItemName = SpacewarID == 0UL && DepotID > 346110U ? DepotNames[DepotID] : $"mod {SpacewarID}";
            Log($"Validating {ItemName} files...");
            SetStatus("Validating files...", YellowBrush);
            IsValidating = true;
            Progress.Total = Manifest.Files.Count;
            Current.Dispatcher.Invoke(ProgressBar.SetNumericMode);
            Progress.Current = StartOffset;
            if (Changes is null)
                Changes = new List<FileEntry>();
            Task[] ValidationTasks = new Task[4];
            for (int Iterator = 0; Iterator < 4; Iterator++)
                ValidationTasks[Iterator] = Factory.StartNew(ValidationJob, new object[] { Manifest.Files, Changes, StartOffset + Iterator });
            WaitAll(ValidationTasks);
            bool Paused = Cancellator.IsCancellationRequested;
            string FinishType = Paused ? "paused" : "complete";
            Log($"Validation {FinishType}: {FilesUpToDate} files are up to date, {FilesOutdated} are outdated and {FilesMissing} are missing");
            if (Changes.Count != 0)
            {
                using (FileStream CacheWriter = Create($@"{DownloadsDirectory}\{ItemCompound}{LatestManifestID}.vcache"))
                {
                    CacheWriter.WriteByte((byte)(Paused ? 1 : 0));
                    CacheWriter.Write(GetBytes(DownloadSize), 0, 8);
                    CacheWriter.Write(GetBytes(InstallSize), 0, 8);
                    CacheWriter.Write(GetBytes(FilesUpToDate), 0, 4);
                    CacheWriter.Write(GetBytes(FilesOutdated), 0, 4);
                    CacheWriter.Write(GetBytes(FilesMissing), 0, 4);
                    foreach (FileEntry File in Changes)
                        File.WriteToFile(CacheWriter);
                }
                Log($"Validation cache written to {ItemCompound}{LatestManifestID}.vcache");
            }
            IsValidating = false;
            return Paused ? null : Changes;
        }
        internal void DownloadMod(ulong ModID, ulong SpacewarModID, ref ModDetails Details, ref ModDetails SpacewarDetails)
        {
            if (ModLocks.Contains(SpacewarID = SpacewarModID))
                return;
            else
                ModLocks.Add(SpacewarID);
            FilesUpToDate = FilesOutdated = FilesMissing = 0;
            InstallSize = DownloadSize = 0L;
            BaseDownloadPath = $@"{DownloadsDirectory}\346110.{this.ModID = ModID}";
            Cancellator = new CancellationTokenSource();
            SetStatus("Connecting to Steam network...", YellowBrush);
            if (Connect())
            {
                SetStatus("Requesting data from Steam...", YellowBrush);
                if ((LatestManifestID = GetManifestIDForMod(346110U, ModID)) == 0UL)
                    SetStatus("Timeout requesting data, try again", DarkRed);
                else
                {
                    CDNClient = new CDNClient(DepotID, Progress, ProgressBar, SetStatus, ModID);
                    List<FileEntry> Files = CDNClient.GetManifests(LatestManifestID, out _).Files;
                    Log($"{Files.Count} files are going to be downloaded with a total size {ConvertBytes(InstallSize = Files.Sum(File => File.Size))} and download size {ConvertBytes(DownloadSize = Files.Sum(File => File.Chunks.Sum(Chunk => (long)Chunk.CompressedSize)))}");
                    Log("Preallocating files...");
                    SetStatus("Preallocating", YellowBrush);
                    Preallocate(Files);
                    SetStatus("Downloading mod files", YellowBrush);
                    IsDownloading = true;
                    CDNClient.DownloadChanges(Files, DownloadSize, Cancellator.Token);
                    if (Cancellator.IsCancellationRequested || CDNClient.DownloadFailed)
                    {
                        IsDownloading = false;
                        if (!CDNClient.DownloadFailed)
                            SetStatus("Download paused", DarkGreen);
                    }
                    else
                    {
                        IsDownloading = false;
                        Log("Mod download complete, proceeding to install");
                        SetStatus("Installing mod", YellowBrush);
                        InstallChanges(Files);
                        string InstalledModPath = $@"{Steam.WorkshopPath}\{SpacewarID}";
                        WriteAllText($@"{InstalledModPath}\OriginID.txt", ModID.ToString());
                        Mod Mod = Mods.Find(ListMod => ListMod.ID == SpacewarModID);
                        if (!(Mod is null))
                            Mods.Remove(Mod);
                        Mod = new Mod(InstalledModPath, null)
                        {
                            Details = SpacewarDetails,
                            OriginDetails = Details
                        };
                        Mod.Install(Progress, ProgressBar);
                        Mods.Add(Mod);
                        Log("Mod installation complete");
                        FilesUpToDate++;
                    }
                }
            }
            else
                SetStatus("Failed to connect to Steam network", DarkRed);
            if (ModLocks.Contains(SpacewarID))
                ModLocks.Remove(SpacewarID);
            Current.Dispatcher.Invoke(Finish);
        }
        internal void Pause() => Cancellator?.Cancel();
        internal void ReleaseLock()
        {
            if (SpacewarID == 0UL)
                DepotLocks[DepotID] = false;
            else if (ModLocks.Contains(SpacewarID))
                ModLocks.Remove(SpacewarID);
        }
        internal void Update(bool DoValidate)
        {
            if (DepotLocks[DepotID])
                return;
            else
                DepotLocks[DepotID] = true;
            ValidationFailed = false;
            Cancellator = new CancellationTokenSource();
            FilesToDelete = null;
            FilesUpToDate = FilesOutdated = FilesMissing = 0;
            InstallSize = DownloadSize = 0L;
            SetStatus("Connecting to Steam network...", YellowBrush);
            if (Connect())
            {
                SetStatus("Requesting data from Steam...", YellowBrush);
                if ((LatestManifestID = GetManifestIDForDepot(DepotID)) == 0UL)
                    SetStatus("Timeout requesting data, try again", DarkRed);
                else
                {
                    CDNClient = new CDNClient(DepotID, Progress, ProgressBar, SetStatus);
                    DepotManifest OldManifest = null;
                    bool Finish = false;
                    List<FileEntry> Changes;
                    if (!(DepotID == 346111U ? Directory.Exists($@"{Game.Path}\ShooterGame") : DLCs.Where(DLC => DLC.DepotID == DepotID).FirstOrDefault()?.IsInstalled ?? true))
                    {
                        Changes = CDNClient.GetManifests(LatestManifestID, out _).Files;
                        FilesMissing = Changes.Count;
                        DownloadSize = Changes.Sum(File => File.Chunks.Sum(Chunk =>(long)Chunk.CompressedSize));
                        InstallSize = Changes.Sum(File => File.Size);
                        Log("Installation not found, staging the entire manifest file listing to be installed");
                    }
                    else if ((Changes = ReadValidationCache(out bool IsIncomplete)) is null)
                    {
                        if (!(DoValidate || Game.UpdateAvailable) && DepotID == 346111U && FileExists($@"{ManifestsDirectory}\346111-{LatestManifestID}.manifest") && Directory.EnumerateFiles(ManifestsDirectory).Where(File => File.Contains("346111-") && File.EndsWith(".manifest")).Count() == 1)
                        {
                            SetStatus("Your game is already up to date ", DarkGreen);
                            Finish = true;
                        }
                        else
                        {
                            DepotManifest LatestManifest = CDNClient.GetManifests(LatestManifestID, out OldManifest);
                            Changes = DoValidate || OldManifest is null ? Validate(LatestManifest) : ComputeChanges(OldManifest, LatestManifest);
                            if (Changes is null)
                            {
                                IsValidating = false;
                                if (!ValidationFailed)
                                    SetStatus("Validation paused", DarkGreen);
                                Finish = true;
                            }
                        }
                    }
                    else if (IsIncomplete)
                    {
                        Log("Incomplete validation cache found, preparing to continue validation");
                        Validate(CDNClient.GetManifests(LatestManifestID, out _), Changes, FilesUpToDate + FilesOutdated + FilesMissing);
                    }
                    else
                        Log("Validation cache found, using it instead of manifests");
                    if (!Finish)
                    {
                        if (Changes.Count == 0)
                        {
                            if (!(OldManifest is null) && FileExists(OldManifest.Path))
                                Delete(OldManifest.Path);
                            Log("No changes required, finishing up");
                            SetStatus($"Your {DepotNames[DepotID]} is already up to date ", DarkGreen);
                        }
                        else
                        {
                            Log($"{FilesMissing + FilesOutdated} files are going to be downloaded with a total size {ConvertBytes(InstallSize)} and download size {ConvertBytes(DownloadSize)}");
                            Log("Preallocating files...");
                            SetStatus("Preallocating", YellowBrush);
                            Preallocate(Changes);
                            SetStatus("Downloading updated files", YellowBrush);
                            IsDownloading = true;
                            CDNClient.DownloadChanges(Changes, DownloadSize, Cancellator.Token);
                            if (Cancellator.IsCancellationRequested || CDNClient.DownloadFailed)
                            {
                                IsDownloading = false;
                                if (!CDNClient.DownloadFailed)
                                    SetStatus("Download paused", DarkGreen);
                            }
                            else
                            {
                                IsDownloading = false;
                                Log("Download complete, installing...");
                                SetStatus("Installing update", YellowBrush);
                                InstallChanges(Changes);
                                if (!(OldManifest is null) && FileExists(OldManifest.Path))
                                    Delete(OldManifest.Path);
                                Log("Installation complete!");
                                if (DepotID == 346111U)
                                {
                                    Game.UpdateAvailable = Game.IsCorrupted = false;
                                    Current.Dispatcher.Invoke(() => Instance.MWindow.SetCurrentVersion(Game.Version ?? "Latest", DarkGreen));
                                }
                                SetStatus($"Your {DepotNames[DepotID]} has been successfully updated", DarkGreen);
                            }
                        }
                    }
                }
            }
            else
                SetStatus("Failed to connect to Steam network", DarkRed);
            DepotLocks[DepotID] = false;
            Current.Dispatcher.Invoke(Finish);
        }
        internal void UpdateMod(bool DoValidate, ulong ModID, ulong SpacewarID)
        {
            if (ModLocks.Contains(SpacewarID))
                return;
            else
                ModLocks.Add(SpacewarID);
            this.SpacewarID = SpacewarID;
            ValidationFailed = false;
            Cancellator = new CancellationTokenSource();
            FilesToDelete = null;
            FilesUpToDate = FilesOutdated = FilesMissing = 0;
            InstallSize = DownloadSize = 0L;
            BaseDownloadPath = $@"{DownloadsDirectory}\{DepotID}.{this.ModID = ModID}";
            SetStatus("Connecting to Steam network...", YellowBrush);
            if (Connect())
            {
                SetStatus("Requesting data from Steam...", YellowBrush);
                if ((LatestManifestID = GetManifestIDForMod(DepotID, ModID)) == 0UL)
                    SetStatus("Timeout requesting data, try again", DarkRed);
                else
                {
                    CDNClient = new CDNClient(DepotID, Progress, ProgressBar, SetStatus, ModID);
                    DepotManifest OldManifest = null;
                    List<FileEntry> Changes = ReadValidationCache(out bool IsIncomplete);
                    bool Finish = false;
                    if (Changes is null)
                    {
                        DepotManifest LatestManifest = CDNClient.GetManifests(LatestManifestID, out OldManifest);
                        Changes = DoValidate || OldManifest is null ? Validate(LatestManifest) : ComputeChanges(OldManifest, LatestManifest);
                        if (Changes is null)
                        {
                            IsValidating = false;
                            if (!ValidationFailed)
                                SetStatus("Validation paused", DarkGreen);
                            Finish = true;
                        }
                    }
                    else if (IsIncomplete)
                    {
                        Log("Incomplete validation cache found, preparing to continue validation");
                        Validate(CDNClient.GetManifests(LatestManifestID, out _), Changes, FilesUpToDate + FilesOutdated + FilesMissing);
                    }
                    else
                        Log("Validation cache found, using it instead of manifests");
                    if (!Finish)
                    {
                        if (Changes.Count == 0)
                        {
                            if (!(OldManifest is null) && FileExists(OldManifest.Path))
                                Delete(OldManifest.Path);
                            Log("No changes required, finishing up");
                            SetStatus($"Mod {ModID} is already up to date", DarkGreen);
                        }
                        else
                        {
                            Log($"{FilesMissing + FilesOutdated} files are going to be downloaded with a total size {ConvertBytes(InstallSize)} and download size {ConvertBytes(DownloadSize)}");
                            Log("Preallocating files...");
                            SetStatus("Preallocating", YellowBrush);
                            Preallocate(Changes);
                            SetStatus("Downloading updated files", YellowBrush);
                            IsDownloading = true;
                            CDNClient.DownloadChanges(Changes, DownloadSize, Cancellator.Token);
                            if (Cancellator.IsCancellationRequested || CDNClient.DownloadFailed)
                            {
                                IsDownloading = false;
                                if (!CDNClient.DownloadFailed)
                                    SetStatus("Download paused", DarkGreen);
                            }
                            else
                            {
                                IsDownloading = false;
                                Log("Download complete, installing...");
                                SetStatus("Installing update", YellowBrush);
                                InstallChanges(Changes);
                                SetStatus("Commiting installed changes", YellowBrush);
                                Mods.Find(Mod => Mod.ID == SpacewarID).Install(Progress, ProgressBar);
                                if (!(OldManifest is null) && FileExists(OldManifest.Path))
                                    Delete(OldManifest.Path);
                                Log("Installation complete!");
                                SetStatus("Mod has been successfully updated", DarkGreen);
                            }
                        }
                    }
                }
            }
            else
                SetStatus("Failed to connect to Steam network", DarkRed);
            if (ModLocks.Contains(SpacewarID))
                ModLocks.Remove(SpacewarID);
            Current.Dispatcher.Invoke(Finish);
        }
    }
}