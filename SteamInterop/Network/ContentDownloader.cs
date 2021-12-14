using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Media;
using TEKLauncher.ARK;
using TEKLauncher.Controls;
using TEKLauncher.Data;
using TEKLauncher.SteamInterop.Network.CDN;
using TEKLauncher.SteamInterop.Network.Manifest;
using TEKLauncher.Utils;
using static System.BitConverter;
using static System.Math;
using static System.IO.File;
using static System.Windows.Application;
using static System.Windows.Media.Brushes;
using static TEKLauncher.App;
using static TEKLauncher.ARK.DLCManager;
using static TEKLauncher.ARK.ModManager;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.Data.Settings;
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
        private int ThreadsCount;
        private ulong LatestManifestID, ModID, SpacewarID;
        private string BaseDownloadPath, ETAString;
        private CancellationTokenSource Cancellator;
        private CDNClient CDNClient;
        private Dictionary<string, long> RelocSizes;
        private Dictionary<string, List<Relocation>> Relocations;
        private Exception CaughtException;
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
        private static readonly string NAString = LocString(LocCode.NA);
        private static readonly List<uint> DepotLocks = new List<uint>();
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
            },
            [1691801U] = new byte[]
            {
                0xDB, 0x89, 0xFE, 0x99, 0x71, 0x0B, 0xE8, 0xD8, 0x0B, 0x85, 0x37, 0x7D, 0x37, 0x21, 0x04, 0x87,
                0xDB, 0xCE, 0x8B, 0x9B, 0xFB, 0x8E, 0x90, 0x76, 0x7F, 0x97, 0xE1, 0x38, 0xD3, 0xE0, 0x20, 0x6E
            }
        };
        internal delegate void FinishDelegate();
        internal delegate void SetStatusDelegate(string Text, SolidColorBrush Color);
        private void InstallChanges(List<FileEntry> Files)
        {
            if (Game.IsRunning && ModID == 0UL)
                throw new ValidatorException(LocString(LocCode.CantInstallUpdate));
            else
            {
                string BaseLocalPath = ModID == 0UL ? Game.Path : $@"{Steam.WorkshopPath}\{SpacewarID}", ItemCompound = ModID == 0UL ? $"{DepotID}-" : $"{DepotID}.{ModID}-";
                if (ModID != 0UL && BaseDownloadPath[0] != BaseLocalPath[0])
                {
                    Directory.CreateDirectory(BaseLocalPath);
                    long DiskFreeSpace = GetFreeSpace(BaseLocalPath);
                    if (InstallSize > DiskFreeSpace)
                    {
                        Directory.Delete(BaseLocalPath);
                        throw new ValidatorException(string.Format(LocString(LocCode.NotEnoughSpace), ConvertBytesLoc(InstallSize - DiskFreeSpace)));
                    }
                }
                if (!(Relocations is null))
                {
                    Log("Applying relocations...");
                    Progress.Total = RelocSizes.Values.Sum() << 1;
                    Current.Dispatcher.Invoke(ProgressBar.SetPercentageMode);
                    SetStatus(LocString(LocCode.ApplyingRelocs), YellowBrush);
                    string CachePath = $@"{BaseDownloadPath}\Relocs.trf";
                    using (FileStream Cache = Open(CachePath, FileMode.Create, FileAccess.ReadWrite))
                        foreach (string Name in RelocSizes.Keys)
                        {
                            string FilePath = $@"{BaseLocalPath}\{Name}";
                            if (!FileExists(FilePath))
                                throw new ValidatorException(LocString(LocCode.InstallationCorrupted));
                            using (FileStream File = Open(FilePath, FileMode.Open, FileAccess.ReadWrite))
                            {
                                List<Relocation> Relocs = Relocations[Name];
                                foreach (Relocation Reloc in Relocs)
                                {
                                    File.Position = Reloc.OldOffset;
                                    byte[] Buffer = new byte[Reloc.Size];
                                    File.Read(Buffer, 0, Reloc.Size);
                                    Cache.Write(Buffer, 0, Reloc.Size);
                                    Progress.IncreaseNoETA(Reloc.Size);
                                }
                                Cache.Position = 0L;
                                File.SetLength(RelocSizes[Name]);
                                foreach (Relocation Reloc in Relocs)
                                {
                                    byte[] Buffer = new byte[Reloc.Size];
                                    Cache.Read(Buffer, 0, Reloc.Size);
                                    File.Position = Reloc.NewOffset;
                                    File.Write(Buffer, 0, Reloc.Size);
                                    Progress.IncreaseNoETA(Reloc.Size);
                                }
                                Cache.Position = 0L;
                                Cache.SetLength(0L);
                            }
                        }
                    Log("Relocations applied");
                }
                Progress.Total = Files.Count;
                Current.Dispatcher.Invoke(ProgressBar.SetNumericMode);
                SetStatus(LocString(LocCode.InstallingUpd), YellowBrush);
                foreach (FileEntry File in Files)
                {
                    string DestinationPath = $@"{BaseLocalPath}\{File.Name}", SourcePath = $@"{BaseDownloadPath}\{File.Name}";
                    if (!FileExists(SourcePath))
                    {
                        DeletePath($@"{DownloadsDirectory}\{ItemCompound}{LatestManifestID}.vcache");
                        throw new ValidatorException(LocString(LocCode.InstallFailedVCDupe));
                    }
                    if (File.IsSliced)
                    {
                        if (!Directory.Exists(Path.GetDirectoryName(DestinationPath)))
                            throw new ValidatorException(LocString(LocCode.InstallationCorrupted));
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
                        try { Directory.CreateDirectory(Path.GetDirectoryName(DestinationPath)); }
                        catch (ArgumentException Exception) { throw new ArgumentException($"{Exception.Message} Evaluated path: \"{DestinationPath}\""); }//Unitl finding out what exactly causes this crash
                        try { Move(SourcePath, DestinationPath); }
                        catch (IOException Exception)
                        {
                            Log($@"Install failed for depot {DepotID}: {Exception.Message} (""{SourcePath}"">""{DestinationPath}"")");
                            if (Exception is FileNotFoundException)
                                throw new ValidatorException(LocString(LocCode.InstallFailedVCDupe));
                        }
                    }
                    Progress.Increase();
                }
                if (!(FilesToDelete is null))
                    foreach (string File in FilesToDelete)
                        DeletePath($@"{BaseLocalPath}\{File}");
                DeletePath($@"{DownloadsDirectory}\{ItemCompound}{LatestManifestID}.vcache");
                DeleteDirectory(BaseDownloadPath);
                if (DepotID == 346111U)
                {
                    string BELIni = $@"{BaseLocalPath}\Engine\Config\BaseEditorLayout.ini", BIni = $@"{BaseLocalPath}\Engine\Config\Base.ini";
                    if (!FileExists(BELIni))
                        using (FileStream Writer = Create(BELIni))
                            Writer.SetLength(0L);
                    if (!FileExists(BIni))
                        using (FileStream Writer = Create(BIni))
                            Writer.SetLength(0L);
                }
            }
        }
        private void Preallocate(List<FileEntry> Files)
        {
            if (BaseDownloadPath.Length > 255)
                throw new ValidatorException(LocString(LocCode.PathTooLong));
            if (!Directory.Exists(BaseDownloadPath))
            {
                Directory.CreateDirectory(BaseDownloadPath);
                long DiskFreeSpace = GetFreeSpace(BaseDownloadPath);
                if (InstallSize > DiskFreeSpace)
                {
                    Directory.Delete(BaseDownloadPath);
                    throw new ValidatorException(string.Format(LocString(LocCode.NotEnoughSpace), ConvertBytesLoc(InstallSize - DiskFreeSpace)));
                }
            }
            Progress.Total = Files.Count;
            Current.Dispatcher.Invoke(ProgressBar.SetNumericMode);
            foreach (FileEntry File in Files)
            {
                string FilePath = $@"{BaseDownloadPath}\{File.Name}";
                if (FilePath.Length > 255)
                    throw new ValidatorException(LocString(LocCode.PathTooLong));
                try { Directory.CreateDirectory(Path.GetDirectoryName(FilePath)); }
                catch (ArgumentException)
                {
                    DeletePath($@"{DownloadsDirectory}\{DepotID}-{LatestManifestID}.vcache");
                    throw new ValidatorException(LocString(LocCode.ManifestDecryptedIncorrectly));
                }
                using (FileStream Stream = OpenWrite(FilePath))
                    Stream.SetLength(File.IsSliced ? File.Chunks.Sum(Chunk => Chunk.UncompressedSize) : File.Size);
                Progress.Increase();
            }
            Log("Preallocation complete, proceeding to download...");
        }
        private void ProgressUpdatedHandler() => SetStatus(string.Format(ETAString, Progress.ETA < 0L ? NAString : ConvertTime(Progress.ETA)), YellowBrush);
        private void ValidationJob(object Args)
        {
            object[] ArgsArray = (object[])Args;
            List<FileEntry> Files = (List<FileEntry>)ArgsArray[0];
            List<FileEntry> Changes = (List<FileEntry>)ArgsArray[1];
            int Total = Files.Count;
            try
            {
                string BaseLocalPath = ModID == 0UL ? Game.Path : $@"{Steam.WorkshopPath}\{SpacewarID}";
                using (SHA1 SHA = SHA1.Create())
                    for (int Iterator = (int)ArgsArray[2]; Iterator < Total; Iterator += ThreadsCount)
                    {
                        if (Cancellator.IsCancellationRequested)
                            break;
                        FileEntry File = Files[Iterator];
                        lock (ProgressLock)
                            if (!(Changes.Find(ChangeFile => ChangeFile.Name == File.Name) is null))
                                continue;
                        string LocalFile = $@"{BaseLocalPath}\{File.Name}";
                        if (FileExists(LocalFile))
                            using (FileStream LocalFileStream = OpenRead(LocalFile))
                            {
                                byte[] FileHash;
                                try { FileHash = SHA.ComputeHash(LocalFileStream); }
                                catch { FileHash = new byte[0]; }
                                if (FileHash.SequenceEqual(File.Checksum))
                                    lock (ProgressLock)
                                    {
                                        FilesUpToDate++;
                                        Progress.Increase(File.Size);
                                    }
                                else
                                {
                                    FileEntry ChangeFile = new FileEntry { Checksum = File.Checksum, Size = File.Size, Name = File.Name, IsSliced = true };
                                    long LocalSize = LocalFileStream.Length, FileDownloadSize = 0L, FileInstallSize = 0L;
                                    foreach (ChunkEntry Chunk in File.Chunks)
                                    {
                                        if (Chunk.Offset + Chunk.UncompressedSize > LocalSize)
                                        {
                                            FileDownloadSize += Chunk.CompressedSize;
                                            FileInstallSize += Chunk.UncompressedSize;
                                            ChangeFile.Chunks.Add(Chunk);
                                        }
                                        else
                                        {
                                            byte[] Buffer = new byte[Chunk.UncompressedSize];
                                            LocalFileStream.Position = Chunk.Offset;
                                            LocalFileStream.Read(Buffer, 0, Chunk.UncompressedSize);
                                            if (ComputeAdlerHash(Buffer) != Chunk.Checksum)
                                            {
                                                FileDownloadSize += Chunk.CompressedSize;
                                                FileInstallSize += Chunk.UncompressedSize;
                                                ChangeFile.Chunks.Add(Chunk);
                                            }
                                        }
                                        lock (ProgressLock)
                                            Progress.Increase(Chunk.UncompressedSize);
                                    }
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
                                Progress.Increase(File.Size);
                            }
                        }
                    }
            }
            catch (Exception Exception)
            {
                Cancellator.Cancel();
                CaughtException = Exception is IOException ? new ValidatorException(Exception.Message) : Exception;
            }
        }
        private int GIDOffsetComparison(ChunkEntry A, ChunkEntry B)
        {
            int Difference = ((IStructuralComparable)A.GID).CompareTo(B.GID, StructuralComparisons.StructuralComparer);
            if (Difference == 0)
                Difference = A.Offset.CompareTo(B.Offset);
            return Difference;
        }
        private int OffsetComparison(ChunkEntry A, ChunkEntry B) => A.Offset.CompareTo(B.Offset);
        private string GetDepotName(uint DepotID) => DepotID == 346111U ? LocString(LocCode.Game) : $"{DLCs.First(DLC => DLC.DepotID == DepotID).Name} DLC";
        private List<FileEntry> ReadValidationCache(out bool IsIncomplete)
        {
            string ItemCompound = ModID == 0UL ? $"{DepotID}-" : $"{DepotID}.{ModID}-", VCacheFile = $@"{DownloadsDirectory}\{ItemCompound}{LatestManifestID}.vcache";
            foreach (string VCache in Directory.EnumerateFiles(DownloadsDirectory).Where(File => File.Contains(ItemCompound) && File.EndsWith(".vcache") && File != VCacheFile))
                Delete(VCache);
            if (GetFileSize(VCacheFile) > 7L)
                using (FileStream CacheReader = Open(VCacheFile, FileMode.Open, FileAccess.ReadWrite))
                {
                    byte[] Buffer = new byte[8];
                    CacheReader.Position = CacheReader.Length - 4L;
                    CacheReader.Read(Buffer, 0, 4);
                    if (Buffer[0] == 0xFF && Buffer[1] == 0xFF && Buffer[2] == 0xFF && Buffer[3] == 0xFF)
                    {
                        CacheReader.Position -= 8L;
                        CacheReader.Read(Buffer, 0, 8);
                        byte[] CRCBuffer = new byte[4];
                        Array.Copy(Buffer, CRCBuffer, 4);
                        CacheReader.Position = 0L;
                        CacheReader.SetLength(CacheReader.Length - 8L);
                        using (CRC32 CRC = new CRC32())
                            if (!CRC.ComputeHash(CacheReader).SequenceEqual(CRCBuffer))
                            {
                                CacheReader.Close();
                                DeletePath(VCacheFile);
                                IsIncomplete = false;
                                return null;
                            }
                        CacheReader.Write(Buffer, 0, 8);
                    }
                    CacheReader.Position = 0L;
                    IsIncomplete = CacheReader.ReadByte() == 1;
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
            Log("Computing changes between old and latest manifest...");
            SetStatus(LocString(LocCode.ComputingChanges), YellowBrush);
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
                    DownloadSize += File.Chunks.Sum(Chunk => (long)Chunk.CompressedSize);
                    InstallSize += File.Size;
                    Changes.Add(File);
                    Offset++;
                    Iterator--;
                    continue;
                }
                if (!File.Checksum.SequenceEqual(OldFile.Checksum))
                {
                    List<Relocation> RelocatedChunks = null;
                    FileEntry ChangeFile = new FileEntry { Checksum = File.Checksum, Size = File.Size, Name = File.Name, IsSliced = true };
                    int ChunksCount = File.Chunks.Count, OldChunksCount = OldFile.Chunks.Count;
                    File.Chunks.Sort(GIDOffsetComparison);
                    OldFile.Chunks.Sort(GIDOffsetComparison);
                    int CIterator = 0, COffset = 0;
                    for (; CIterator < OldChunksCount && CIterator + COffset < ChunksCount; CIterator++)
                    {
                        ChunkEntry Chunk = File.Chunks[CIterator + COffset], OldChunk = OldFile.Chunks[CIterator];
                        int CDifference = ((IStructuralComparable)OldChunk.GID).CompareTo(Chunk.GID, StructuralComparisons.StructuralComparer);
                        if (CDifference < 0)
                        {
                            COffset--;
                            continue;
                        }
                        else if (CDifference > 0)
                        {
                            DownloadSize += Chunk.CompressedSize;
                            InstallSize += Chunk.UncompressedSize;
                            ChangeFile.Chunks.Add(Chunk);
                            COffset++;
                            CIterator--;
                            continue;
                        }
                        if (Chunk.Offset != OldChunk.Offset)
                        {
                            if (RelocatedChunks is null)
                                RelocatedChunks = new List<Relocation>();
                            InstallSize += Chunk.UncompressedSize;
                            RelocatedChunks.Add(new Relocation { Size = OldChunk.UncompressedSize, OldOffset = OldChunk.Offset, NewOffset = Chunk.Offset });
                        }
                    }
                    for (int CIterator1 = CIterator + COffset; CIterator1 < ChunksCount; CIterator1++)
                    {
                        ChunkEntry Chunk = File.Chunks[CIterator1];
                        DownloadSize += Chunk.CompressedSize;
                        InstallSize += Chunk.UncompressedSize;
                        ChangeFile.Chunks.Add(Chunk);
                    }
                    File.Chunks.Sort(OffsetComparison);
                    OldFile.Chunks.Sort(OffsetComparison);
                    ChangeFile.Chunks.Sort(OffsetComparison);
                    bool HasChanges = ChangeFile.Chunks.Count != 0, HasRelocs = !(RelocatedChunks is null);
                    if (HasChanges || HasRelocs)
                        FilesOutdated++;
                    if (HasChanges)
                        Changes.Add(ChangeFile);
                    if (HasRelocs)
                    {
                        if (Relocations is null)
                        {
                            RelocSizes = new Dictionary<string, long>();
                            Relocations = new Dictionary<string, List<Relocation>>();
                        }
                        RelocSizes[File.Name] = File.Size;
                        Relocations[File.Name] = RelocatedChunks;
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
                DownloadSize += File.Chunks.Sum(Chunk => (long)Chunk.CompressedSize);
                InstallSize += File.Size;
                Changes.Add(File);
            }
            Log($"Computing changes complete: {FilesOutdated} files updated and {FilesMissing} new files");
            return Changes;
        }
        private List<FileEntry> Validate(DepotManifest Manifest, List<FileEntry> Changes = null, int StartOffset = 0)
        {
            string ItemCompound = ModID == 0UL ? $"{DepotID}-" : $"{DepotID}.{ModID}-", ItemName = SpacewarID == 0UL && DepotID > 346110U ? GetDepotName(DepotID) : $"mod {SpacewarID}";
            Log($"Validating {ItemName} files...");
            IsValidating = true;
            Progress.Total = Manifest.Files.Sum(File => File.Size);
            Current.Dispatcher.Invoke(ProgressBar.SetPercentageMode);
            for (int Iterator = 0; Iterator < StartOffset; Iterator++)
                Progress.Current += Manifest.Files[Iterator].Size;
            Progress.IncreaseNoETA(0L);
            if (Changes is null)
                Changes = new List<FileEntry>();
            CaughtException = null;
            Thread[] ValidationThreads = new Thread[ThreadsCount = ValThreadsCount];
            ETAString = LocString(LocCode.ValidatingFiles);
            Current.Dispatcher.Invoke(() => ProgressBar.ProgressUpdated += ProgressUpdatedHandler);
            for (int Iterator = 0; Iterator < ThreadsCount; Iterator++)
            {
                Thread ValidationThread = new Thread(ValidationJob);
                ValidationThread.Start(new object[] { Manifest.Files, Changes, StartOffset + Iterator });
                ValidationThreads[Iterator] = ValidationThread;
            }
            foreach (Thread ValidationThread in ValidationThreads)
                ValidationThread.Join();
            Current.Dispatcher.Invoke(() => ProgressBar.ProgressUpdated -= ProgressUpdatedHandler);
            if (!(CaughtException  is null))
            {
                IsValidating = false;
                throw CaughtException;
            }
            bool Paused = Cancellator.IsCancellationRequested;
            string FinishType = Paused ? "paused" : "complete";
            Log($"Validation {FinishType}: {FilesUpToDate} files are up to date, {FilesOutdated} are outdated and {FilesMissing} are missing");
            if (Paused || Changes.Count != 0)
            {
                using (FileStream CacheWriter = Open($@"{DownloadsDirectory}\{ItemCompound}{LatestManifestID}.vcache", FileMode.Create, FileAccess.ReadWrite))
                {
                    CacheWriter.WriteByte((byte)(Paused ? 1 : 0));
                    CacheWriter.Write(GetBytes(DownloadSize), 0, 8);
                    CacheWriter.Write(GetBytes(InstallSize), 0, 8);
                    CacheWriter.Write(GetBytes(FilesUpToDate), 0, 4);
                    CacheWriter.Write(GetBytes(FilesOutdated), 0, 4);
                    CacheWriter.Write(GetBytes(FilesMissing), 0, 4);
                    foreach (FileEntry File in Changes)
                        File.WriteToFile(CacheWriter);
                    CacheWriter.Position = 0L;
                    byte[] CRCHash;
                    using (CRC32 CRC = new CRC32())
                        CRCHash = CRC.ComputeHash(CacheWriter);
                    CacheWriter.Write(CRCHash, 0, 4);
                    CacheWriter.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, 0, 4);
                }
                Log($"Validation cache written to {ItemCompound}{LatestManifestID}.vcache");
            }
            IsValidating = false;
            return Paused ? null : Changes;
        }
        internal void DownloadMod(ulong ModID, ulong SpacewarModID, ref ModDetails Details, ref ModDetails SpacewarDetails)
        {
            if (ModLocks.Contains(SpacewarID = SpacewarModID))
            {
                SetStatus(LocString(LocCode.ModAlrUpdating), DarkRed);
                return;
            }
            else
                ModLocks.Add(SpacewarID);
            FilesUpToDate = FilesOutdated = FilesMissing = 0;
            InstallSize = DownloadSize = 0L;
            BaseDownloadPath = $@"{DownloadsDirectory}\346110.{this.ModID = ModID}";
            Cancellator = new CancellationTokenSource();
            SetStatus(LocString(LocCode.ConnectingToSteam), YellowBrush);
            if (Connect())
            {
                SetStatus(LocString(LocCode.RequestingData), YellowBrush);
                if ((LatestManifestID = GetManifestIDForMod(346110U, ModID)) == 0UL)
                    SetStatus(LocString(LocCode.Timeout), DarkRed);
                else
                {
                    CDNClient = new CDNClient(DepotID, Progress, ProgressBar, SetStatus, ModID);
                    List<FileEntry> Files = CDNClient.GetManifests(LatestManifestID, out _).Files;
                    Log($"{Files.Count} files are going to be downloaded with a total size {ConvertBytes(InstallSize = Files.Sum(File => File.Size))} and download size {ConvertBytes(DownloadSize = Files.Sum(File => File.Chunks.Sum(Chunk => (long)Chunk.CompressedSize)))}");
                    Log("Preallocating files...");
                    SetStatus(LocString(LocCode.Preallocating), YellowBrush);
                    Preallocate(Files);
                    ETAString = LocString(LocCode.DownloadingMod);
                    Current.Dispatcher.Invoke(() => ProgressBar.ProgressUpdated += ProgressUpdatedHandler);
                    IsDownloading = true;
                    Exception CaughtException = CDNClient.DownloadChanges(Files, DownloadSize, Cancellator.Token);
                    Current.Dispatcher.Invoke(() => ProgressBar.ProgressUpdated -= ProgressUpdatedHandler);
                    IsDownloading = false;
                    if (Cancellator.IsCancellationRequested || CDNClient.DownloadFailed)
                    {
                        if (CDNClient.DownloadFailed)
                            throw CaughtException;
                        SetStatus(LocString(LocCode.DwPaused), DarkGreen);
                    }
                    else
                    {
                        Log("Mod download complete, proceeding to install");
                        SetStatus(LocString(LocCode.CInstallingMod), YellowBrush);
                        InstallChanges(Files);
                        WriteAllText($@"{ManifestsDirectory}\{DepotID}.{ModID}-CurrentID.txt", LatestManifestID.ToString());
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
                SetStatus(LocString(LocCode.ConnectToSteamFailed), DarkRed);
            if (ModLocks.Contains(SpacewarID))
                ModLocks.Remove(SpacewarID);
            Current.Dispatcher.Invoke(Finish);
        }
        internal void Pause() => Cancellator?.Cancel();
        internal void ReleaseLock()
        {
            if (SpacewarID == 0UL)
            {
                if (DepotLocks.Contains(DepotID))
                    DepotLocks.Remove(DepotID);
            }
            else if (ModLocks.Contains(SpacewarID))
                ModLocks.Remove(SpacewarID);
        }
        internal void Update(bool DoValidate)
        {
            if (DepotLocks.Contains(DepotID))
            {
                SetStatus(LocString(LocCode.ItemAlrUpdating), DarkRed);
                return;
            }
            else
                DepotLocks.Add(DepotID);
            bool DowngradeMode = Settings.DowngradeMode;
            Cancellator = new CancellationTokenSource();
            RelocSizes = null;
            Relocations = null;
            FilesToDelete = null;
            FilesUpToDate = FilesOutdated = FilesMissing = 0;
            InstallSize = DownloadSize = 0L;
            SetStatus(LocString(LocCode.ConnectingToSteam), YellowBrush);
            if (Connect())
            {
                SetStatus(LocString(LocCode.RequestingData), YellowBrush);
                if ((LatestManifestID = GetManifestIDForDepot(DepotID)) == 0UL)
                    SetStatus(LocString(LocCode.Timeout), DarkRed);
                else
                {
                    CDNClient = new CDNClient(DepotID, Progress, ProgressBar, SetStatus);
                    DepotManifest OldManifest = null;
                    bool Finish = false;
                    List<FileEntry> Changes = null;
                    if (!Directory.Exists(DownloadsDirectory))
                        throw new ValidatorException(LocString(LocCode.NoDownloadsDir));
                    else if (DowngradeMode)
                    {
                        Log("Downgrade mode is enabled");
                        DepotManifest LatestManifest = CDNClient.GetManifests(LatestManifestID, out OldManifest);
                        if (OldManifest is null)
                        {
                            Log("No manifests found to downgrade to");
                            SetStatus(LocString(LocCode.NoPrevManifest), YellowBrush);
                            Finish = true;
                        }
                        else
                            Changes = ComputeChanges(LatestManifest, OldManifest);
                    }
                    else if (!(DepotID == 346111U ? Directory.Exists($@"{Game.Path}\ShooterGame") : DLCs.Where(DLC => DLC.DepotID == DepotID).FirstOrDefault()?.IsInstalled ?? true))
                    {
                        Changes = CDNClient.GetManifests(LatestManifestID, out _).Files;
                        FilesMissing = Changes.Count;
                        DownloadSize = Changes.Sum(File => File.Chunks.Sum(Chunk => (long)Chunk.CompressedSize));
                        InstallSize = Changes.Sum(File => File.Size);
                        Log("Installation not found, staging the entire manifest file listing to be installed");
                    }
                    else if ((Changes = ReadValidationCache(out bool IsIncomplete)) is null)
                    {
                        string CurrentManifestFile = $@"{ManifestsDirectory}\{DepotID}-CurrentID.txt";
                        if (!DoValidate && !(DepotID == 346111U && Game.UpdateAvailable) && FileExists(CurrentManifestFile) && ulong.TryParse(ReadAllText(CurrentManifestFile), out ulong CID) && CID == LatestManifestID)
                        {
                            SetStatus(DepotID == 346111U ? LocString(LocCode.GameAlrUpToDate) : string.Format(LocString(LocCode.AlrUpToDate), GetDepotName(DepotID)), DarkGreen);
                            Finish = true;
                        }
                        else
                        {
                            DepotManifest LatestManifest = CDNClient.GetManifests(LatestManifestID, out OldManifest);
                            Changes = DoValidate || OldManifest is null ? Validate(LatestManifest) : ComputeChanges(OldManifest, LatestManifest);
                            if (Changes is null)
                            {
                                SetStatus(LocString(LocCode.ValidationPaused), DarkGreen);
                                Finish = true;
                            }
                        }
                    }
                    else if (IsIncomplete)
                    {
                        Log("Incomplete validation cache found, preparing to continue validation");
                        if (Validate(CDNClient.GetManifests(LatestManifestID, out _), Changes, FilesUpToDate + FilesOutdated + FilesMissing) is null)
                        {
                            SetStatus(LocString(LocCode.ValidationPaused), DarkGreen);
                            Finish = true;
                        }
                    }
                    else
                        Log("Validation cache found, using it instead of manifests");
                    if (!Finish)
                    {
                        if (Changes.Count == 0)
                        {
                            Log("No changes required, finishing up");
                            if (DepotID == 346111U)
                                Game.UpdateAvailable = Game.IsCorrupted = false;
                            SetStatus(string.Format(LocString(LocCode.AlrUpToDate), GetDepotName(DepotID)), DarkGreen);
                            WriteAllText($@"{ManifestsDirectory}\{DepotID}-CurrentID.txt", LatestManifestID.ToString());
                        }
                        else
                        {
                            Log($"{FilesMissing + FilesOutdated} files are going to be downloaded with a total size {ConvertBytes(InstallSize)} and download size {ConvertBytes(DownloadSize)}");
                            Log("Preallocating files...");
                            SetStatus(LocString(LocCode.Preallocating), YellowBrush);
                            Preallocate(Changes);
                            SetStatus(string.Format(ETAString = LocString(DowngradeMode ? LocCode.DownloadingPrev : LocCode.DownloadingUpd), LocString(LocCode.NA)), YellowBrush);
                            Current.Dispatcher.Invoke(() => ProgressBar.ProgressUpdated += ProgressUpdatedHandler);
                            IsDownloading = true;
                            Exception CaughtException = CDNClient.DownloadChanges(Changes, DownloadSize, Cancellator.Token);
                            Current.Dispatcher.Invoke(() => ProgressBar.ProgressUpdated -= ProgressUpdatedHandler);
                            IsDownloading = false;
                            if (Cancellator.IsCancellationRequested || CDNClient.DownloadFailed)
                            {
                                if (CDNClient.DownloadFailed)
                                    throw CaughtException;
                                SetStatus(LocString(LocCode.DwPaused), DarkGreen);
                            }
                            else
                            {
                                Log("Download complete, installing...");
                                InstallChanges(Changes);
                                Log("Installation complete!");
                                if (DepotID == 346111U)
                                {
                                    if (DowngradeMode)
                                    {
                                        Game.UpdateAvailable = true;
                                        Current.Dispatcher.Invoke(() => Instance.MWindow.SetCurrentVersion(Game.Version ?? LocString(LocCode.Outdated), YellowBrush));
                                    }
                                    else
                                    {
                                        Game.UpdateAvailable = Game.IsCorrupted = false;
                                        Current.Dispatcher.Invoke(() => Instance.MWindow.SetCurrentVersion(Game.Version ?? LocString(LocCode.Latest), DarkGreen));
                                    }
                                }
                                WriteAllText($@"{ManifestsDirectory}\{DepotID}-CurrentID.txt", DowngradeMode ? OldManifest.ID : LatestManifestID.ToString());
                                SetStatus(string.Format(LocString(DowngradeMode ? LocCode.DowngradeSuccess : LocCode.UpdateSuccess), GetDepotName(DepotID)), DarkGreen);
                            }
                        }
                    }
                }
            }
            else
                SetStatus(LocString(LocCode.ConnectToSteamFailed), DarkRed);
            if (DepotLocks.Contains(DepotID))
                DepotLocks.Remove(DepotID);
            Current.Dispatcher.Invoke(Finish);
        }
        internal ulong UpdateMod(bool DoValidate, ulong ModID, ulong SpacewarID)
        {
            if (ModLocks.Contains(this.SpacewarID = SpacewarID))
            {
                SetStatus(LocString(LocCode.ModAlrUpdating), DarkRed);
                return 0UL;
            }
            else
                ModLocks.Add(SpacewarID);
            bool DowngradeMode = Settings.DowngradeMode;
            Cancellator = new CancellationTokenSource();
            RelocSizes = null;
            Relocations = null;
            FilesToDelete = null;
            FilesUpToDate = FilesOutdated = FilesMissing = 0;
            InstallSize = DownloadSize = 0L;
            BaseDownloadPath = $@"{DownloadsDirectory}\{DepotID}.{this.ModID = ModID}";
            SetStatus(LocString(LocCode.ConnectingToSteam), YellowBrush);
            if (Connect())
            {
                SetStatus(LocString(LocCode.RequestingData), YellowBrush);
                if ((LatestManifestID = GetManifestIDForMod(DepotID, ModID)) == 0UL)
                    SetStatus(LocString(LocCode.Timeout), DarkRed);
                else
                {
                    CDNClient = new CDNClient(DepotID, Progress, ProgressBar, SetStatus, ModID);
                    DepotManifest OldManifest = null;
                    List<FileEntry> Changes = null;
                    bool Finish = false;
                    if (DowngradeMode)
                    {
                        Log("Downgrade mode is enabled");
                        DepotManifest LatestManifest = CDNClient.GetManifests(LatestManifestID, out OldManifest);
                        if (OldManifest is null)
                        {
                            Log("No manifests found to downgrade to");
                            SetStatus(LocString(LocCode.NoPrevManifest), YellowBrush);
                            Finish = true;
                        }
                        else
                            Changes = ComputeChanges(LatestManifest, OldManifest);
                    }
                    else if ((Changes = ReadValidationCache(out bool IsIncomplete)) is null)
                    {
                        string CurrentManifestFile = $@"{ManifestsDirectory}\{DepotID}.{ModID}-CurrentID.txt";
                        if (!DoValidate && FileExists(CurrentManifestFile) && ulong.TryParse(ReadAllText(CurrentManifestFile), out ulong CID) && CID == LatestManifestID)
                        {
                            SetStatus(string.Format(LocString(LocCode.ModAlrUpToDate), ModID), DarkGreen);
                            Finish = true;
                        }
                        else
                        {
                            DepotManifest LatestManifest = CDNClient.GetManifests(LatestManifestID, out OldManifest);
                            if ((Changes = DoValidate || OldManifest is null ? Validate(LatestManifest) : ComputeChanges(OldManifest, LatestManifest)) is null)
                            {
                                SetStatus(LocString(LocCode.ValidationPaused), DarkGreen);
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
                            Log("No changes required, finishing up");
                            WriteAllText($@"{ManifestsDirectory}\{DepotID}.{ModID}-CurrentID.txt", LatestManifestID.ToString());
                            SetStatus(string.Format(LocString(LocCode.ModAlrUpToDate), ModID), DarkGreen);
                        }
                        else
                        {
                            Log($"{FilesMissing + FilesOutdated} files are going to be downloaded with a total size {ConvertBytes(InstallSize)} and download size {ConvertBytes(DownloadSize)}");
                            Log("Preallocating files...");
                            SetStatus(LocString(LocCode.Preallocating), YellowBrush);
                            Preallocate(Changes);
                            SetStatus(string.Format(ETAString = LocString(DowngradeMode ? LocCode.DownloadingPrev : LocCode.DownloadingUpd), LocString(LocCode.NA)), YellowBrush);
                            Current.Dispatcher.Invoke(() => ProgressBar.ProgressUpdated += ProgressUpdatedHandler);
                            IsDownloading = true;
                            Exception CaughtException = CDNClient.DownloadChanges(Changes, DownloadSize, Cancellator.Token);
                            Current.Dispatcher.Invoke(() => ProgressBar.ProgressUpdated -= ProgressUpdatedHandler);
                            IsDownloading = false;
                            if (Cancellator.IsCancellationRequested || CDNClient.DownloadFailed)
                            {
                                if (CDNClient.DownloadFailed)
                                    throw CaughtException;
                                SetStatus(LocString(LocCode.DwPaused), DarkGreen);
                            }
                            else
                            {
                                Log("Download complete, installing...");
                                InstallChanges(Changes);
                                SetStatus(LocString(LocCode.CommittingUpd), YellowBrush);
                                (Mods.Find(Mod => Mod.ID == SpacewarID) ?? new Mod($@"{Steam.WorkshopPath}\{SpacewarID}", null)).Install(Progress, ProgressBar);
                                Log("Installation complete!");
                                WriteAllText($@"{ManifestsDirectory}\{DepotID}.{ModID}-CurrentID.txt", DowngradeMode ? OldManifest.ID : LatestManifestID.ToString());
                                SetStatus(LocString(DowngradeMode ? LocCode.ModDowngradeSuccess : LocCode.ModUpdSuccess), DarkGreen);
                            }
                        }
                    }
                }
            }
            else
                SetStatus(LocString(LocCode.ConnectToSteamFailed), DarkRed);
            if (ModLocks.Contains(SpacewarID))
                ModLocks.Remove(SpacewarID);
            Current.Dispatcher.Invoke(Finish);
            return LatestManifestID;
        }
        private struct Relocation
        {
            internal int Size;
            internal long OldOffset, NewOffset;
        }
    }
}