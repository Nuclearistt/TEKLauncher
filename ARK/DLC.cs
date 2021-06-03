using System.IO;
using System.Linq;
using System.Security.Cryptography;
using TEKLauncher.Pages;
using static System.Enum;
using static System.IO.Directory;
using static System.Security.Cryptography.SHA1;
using static System.Windows.Application;
using static TEKLauncher.App;
using static TEKLauncher.ARK.DLCManager;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.ARK
{
    internal class DLC
    {
        internal DLC(string Name, uint DepotID, bool IsMod, bool IsSuffixed)
        {
            ContentDirectory = IsMod ? "Mods" : "Maps";
            SpacelessName = Name.Replace(" ", string.Empty);
            this.DepotID = DepotID;
            Path = $@"{Game.Path}\ShooterGame\Content\{ContentDirectory}\{SpacelessName}";
            string UMapFileName = SpacelessName == "Genesis2" ? "Gen2" : SpacelessName;
            SFCPath = $@"{Game.Path}\ShooterGame\SeekFreeContent\{ContentDirectory}\{SpacelessName}";
            UMapPath = IsSuffixed ? $@"{Path}\{SpacelessName}_P.umap" : $@"{Path}\{UMapFileName}.umap";
            Status = IsInstalled ? Status.CheckingForUpdates : Status.NotInstalled;
            this.Name = Name;
            Code = (MapCode)Parse(typeof(MapCode), SpacelessName);
        }
        internal Status Status;
        private readonly string Path, SFCPath, UMapPath;
        internal readonly uint DepotID;
        internal readonly string ContentDirectory, Name, SpacelessName;
        internal readonly MapCode Code;
        internal bool IsInstalled => FileExists(UMapPath);
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
        internal void SetStatus(Status Status, bool Recursed = false)
        {
            this.Status = Status;
            Current.Dispatcher.Invoke(SetItemStatus);
            if (Recursed)
                return;
            if (Code == MapCode.Genesis)
                GetDLC(MapCode.Genesis2).SetStatus(Status, true);
            else if (Code == MapCode.Genesis2)
                GetDLC(MapCode.Genesis).SetStatus(Status, true);
        }
        internal void Uninstall()
        {
            if (Exists(Path))
                DeleteDirectory(Path);
            if (Exists(SFCPath))
                DeleteDirectory(SFCPath);
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