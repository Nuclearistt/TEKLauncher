using TEKLauncher.Controls;
using TEKLauncher.Tabs;
using TEKLauncher.Windows;

namespace TEKLauncher.ARK;

/// <summary>Represents a mod of the game.</summary>
class Mod
{
    /// <summary>Current status of the mod.</summary>
    Status _status;
    /// <summary>Steam published file ID of the mod.</summary>
    public readonly ulong Id;
    /// <summary>Path to the folder that contains compressed files of the mod.</summary>
    public readonly string CompressedFolderPath;
    /// <summary>Path to the .mod file of the mod.</summary>
    public readonly string ModFilePath;
    /// <summary>Path to the folder that contains uncompressed files of the mod used by the game.</summary>
    public readonly string ModsFolderPath;
    /// <summary>Internal name of the mod extracted from its mod.info file.</summary>
    public readonly string Name;
    /// <summary>List of all mods recognized by the launcher.</summary>
    public static readonly List<Mod> List = new();
    /// <summary>Gets or sets current status of the mod.</summary>
    public Status CurrentStatus
    {
        get => _status;
        set
        {
            _status = value;
            Item?.Dispatcher.Invoke(Item.SetStatus);
        }
    }
    /// <summary>Gets or sets Steam workshop details of the mod.</summary>
    public ModDetails Details { get; set; }
    /// <summary>Gets or sets control that represents the mod in GUI.</summary>
    /// <remarks>This property only has a value if current tab of the main window is Mods tab, otherwise it's <see langword="null"/>.</remarks>
    public ModItem? Item { get; set; }
    /// <summary>Path to the directory that stores compressed file fodlers for all mods.</summary>
    public static string? CompressedModsDirectory { get; private set; }
    /// <summary>Initializes a new mod object with specified ID.</summary>
    /// <param name="id">Steam published file ID of the mod.</param>
    public Mod(ulong id)
    {
        Id = id;
        CompressedFolderPath = $@"{CompressedModsDirectory}\{id}";
        string modInfoFile = $@"{CompressedFolderPath}\mod.info";
        if (File.Exists(modInfoFile))
        {
            using var stream = File.OpenRead(modInfoFile);
            Span<byte> buffer = stackalloc byte[4];
            stream.Read(buffer);
            int stringSize = BitConverter.ToInt32(buffer);
            if (stringSize == 0)
                Name = string.Empty;
            byte[] stringBuffer = new byte[--stringSize];
            stream.Read(stringBuffer);
            Name = Encoding.UTF8.GetString(stringBuffer);
        }
        else
            Name = string.Empty;
        ModsFolderPath = $@"{Game.Path}\ShooterGame\Content\Mods\{Id}";
        ModFilePath = string.Concat(ModsFolderPath, ".mod");
    }
    /// <summary>Uninstalls the mod.</summary>
    public void Delete()
    {
        CurrentStatus = Status.Deleting;
        if (Directory.Exists(CompressedFolderPath))
            Directory.Delete(CompressedFolderPath, true);
        if (Directory.Exists(ModsFolderPath))
            Directory.Delete(ModsFolderPath, true);
        if (File.Exists(ModFilePath))
            File.Delete(ModFilePath);
        string downloadCache = $@"{Steam.Client.DownloadsFolder}\346110.{Id}";
        if (Directory.Exists(downloadCache))
            Directory.Delete(downloadCache, true);
        foreach (string file in Directory.EnumerateFiles(Steam.Client.DownloadsFolder, $"346110.{Id}-*.*"))
            File.Delete(file);
        foreach (string manifest in Directory.EnumerateFiles(Steam.Client.ManifestsFolder, $"346110.{Id}-*.*"))
            File.Delete(manifest);
        Steam.Client.CurrentManifestIds.Remove(new(Id));
        lock (List)
            List.Remove(this);
    }
    /// <summary>Finds all installed mods, gets their details, checks for updates and populates the <see cref="List"/>.</summary>
    public static void InitializeList()
    {
        //Set up Mods directory
        CompressedModsDirectory = $@"{Game.Path}\Mods";
        if (!Directory.Exists(CompressedModsDirectory))
        {
            string workshopDirectory = Path.GetFullPath($@"{Game.Path}\..\..\workshop\content\346110");
            if (Directory.Exists(workshopDirectory))
                Directory.CreateSymbolicLink(CompressedModsDirectory, workshopDirectory);
            else
            {
                Directory.CreateDirectory(CompressedModsDirectory);
                return;
            }
        }
        //Find all mods in there
        lock (List)
        {
            foreach (string modFolder in Directory.EnumerateDirectories(CompressedModsDirectory))
                if (ulong.TryParse(Path.GetFileName(modFolder), out ulong id))
                    List.Add(new(id));
            if (List.Count == 0)
                return;
        }
        //Update the GUI with created list if necessary
        Application.Current.Dispatcher.InvokeAsync(delegate
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            if (mainWindow.TabFrame.Child is ModsTab modsTab)
                modsTab.ReloadList();
        });
        //Try to get mod details from Steam
        ulong[] ids;
        lock (List)
        {
            ids = new ulong[List.Count];
            for (int i = 0; i < ids.Length; i++)
                ids[i] = List[i].Id;
        }
        var details = Steam.CM.Client.GetModDetails(ids);
        bool updatesAvailable = false;
        lock (List)
        {
            foreach (var item in details)
                if (item.Status == 1)
                    List.Find(m => m.Id == item.Id)!.Details = item;
            //Check for updates
            foreach (var mod in List)
                if (mod.Details.LastUpdated > File.GetLastWriteTimeUtc($@"{mod.CompressedFolderPath}\mod.info").Ticks)
                {
                    updatesAvailable = true;
                    mod.CurrentStatus = Status.UpdateAvailable;
                }
        }
        //Update the GUI with the details
        Application.Current.Dispatcher.InvokeAsync(delegate
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            if (mainWindow.TabFrame.Child is ModsTab modsTab)
                modsTab.UpdateDetails();
            if (updatesAvailable)
                Notifications.Add(LocManager.GetString(LocCode.ModUpdatesAvailable), LocManager.GetString(LocCode.Update), delegate
                {
                    if (mainWindow.TabFrame.Child is not ModsTab)
                        mainWindow.Navigate(new ModsTab());
                });
        });
    }
    /// <summary>Defines mod status codes.</summary>
    public enum Status
    {
        Installed,
        UpdateAvailable,
        Updating,
        Deleting
    }
    /// <summary>Represents Steam published file details of a mod.</summary>
    public readonly record struct ModDetails(uint AppId, int Status, long LastUpdated, ulong Id, ulong ManifestId, string Name, string PreviewUrl);
}