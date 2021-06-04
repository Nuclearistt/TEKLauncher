using System.IO;
using System.Linq;
using System.Security.Cryptography;
using TEKLauncher.Pages;
using static System.Enum;
using static System.IO.Directory;
using static System.Security.Cryptography.SHA1;
using static System.Windows.Application;
using static TEKLauncher.App;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.ARK
{
    internal class DLC
    {
        internal DLC(string Name, uint DepotID, bool IsMod, bool IsSuffixed)
        {
            ContentDirectory = IsMod ? "Mods" : "Maps";
            SpacelessName = DepotID == 473857U ? "Genesis" : Name.Replace(" ", string.Empty);
            this.DepotID = DepotID;
            Path = $@"{Game.Path}\ShooterGame\Content\{ContentDirectory}\{SpacelessName}";
            SFCPath = $@"{Game.Path}\ShooterGame\SeekFreeContent\{ContentDirectory}\{SpacelessName}";
            UMapPath = IsSuffixed ? $@"{Path}\{SpacelessName}_P.umap" : $@"{Path}\{SpacelessName}.umap";
            Status = IsInstalled ? Status.CheckingForUpdates : Status.NotInstalled;
            this.Name = Name;
            Code = (MapCode)Parse(typeof(MapCode), SpacelessName);
        }
        internal Status Status;
        private readonly string Path, SFCPath, UMapPath;
        internal readonly uint DepotID;
        internal readonly string ContentDirectory, Name, SpacelessName;
        internal readonly MapCode Code;
        internal bool IsInstalled => Code == MapCode.Genesis ? FileExists(UMapPath) || FileExists($@"{Game.Path}\ShooterGame\Content\Maps\Genesis2\Gen2.umap") : FileExists(UMapPath);
        private void SetItemStatus()
        {
            if (Instance?.CurrentPage is DLCsPage Page)
                Page?.GetItem(Code)?.SetStatus(Status);
        }
        internal void CheckForUpdates(byte[] Checksum)
        {
            if (IsInstalled)
            {
                using (SHA1 SHA = Create())
                using (FileStream ChecksumStream = File.OpenRead(UMapPath))
                    Status = SHA.ComputeHash(ChecksumStream).SequenceEqual(Checksum) ? Status.Installed : Status.UpdateAvailable;
                Current?.Dispatcher?.Invoke(SetItemStatus);
            }
        }
        internal void CheckGenesisForUpdates(byte[] Gen1Checksum, byte[] Gen2Checksum)
        {
            int StatusValue = 0;
            if (FileExists(UMapPath))
            {
                using (SHA1 SHA = Create())
                using (FileStream ChecksumStream = File.OpenRead(UMapPath))
                    StatusValue += SHA.ComputeHash(ChecksumStream).SequenceEqual(Gen1Checksum) ? 2 : 1;
            }
            if (FileExists($@"{Game.Path}\ShooterGame\Content\Maps\Genesis2\Gen2.umap"))
            {
                if (Gen2Checksum is null)
                    StatusValue += 2;
                else
                    using (SHA1 SHA = Create())
                    using (FileStream ChecksumStream = File.OpenRead($@"{Game.Path}\ShooterGame\Content\Maps\Genesis2\Gen2.umap"))
                        StatusValue += SHA.ComputeHash(ChecksumStream).SequenceEqual(Gen2Checksum) ? 2 : 1;
            }
            Status = StatusValue == 0 ? Status.NotInstalled : StatusValue == 4 ? Status.Installed : Status.UpdateAvailable;
            Current?.Dispatcher?.Invoke(SetItemStatus);
        }
        internal void SetStatus(Status Status)
        {
            this.Status = Status;
            Current.Dispatcher.Invoke(SetItemStatus);
        }
        internal void Uninstall()
        {
            if (Exists(Path))
                DeleteDirectory(Path);
            if (Exists(SFCPath))
                DeleteDirectory(SFCPath);
            if (Code == MapCode.Genesis)
            {
                if (Exists($@"{Game.Path}\ShooterGame\Content\Maps\Genesis2"))
                    DeleteDirectory($@"{Game.Path}\ShooterGame\Content\Maps\Genesis2");
                if (Exists($@"{Game.Path}\ShooterGame\SeekFreeContent\Maps\Genesis2"))
                    DeleteDirectory($@"{Game.Path}\ShooterGame\SeekFreeContent\Maps\Genesis2");
            }
            string DownloadCache = $@"{DownloadsDirectory}\{DepotID}";
            if (Exists(DownloadCache))
                DeleteDirectory(DownloadCache);
            foreach (string VCache in EnumerateFiles(DownloadsDirectory).Where(File => File.Contains($"{DepotID}-")))
                File.Delete(VCache);
            foreach (string Manifest in EnumerateFiles(ManifestsDirectory).Where(File => File.Contains($"{DepotID}-")))
                File.Delete(Manifest);
            SetStatus(Status.NotInstalled);
        }
    }
}