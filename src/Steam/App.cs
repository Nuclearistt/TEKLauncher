using System.Diagnostics;
using Microsoft.Win32;
using TEKLauncher.Steam.CM;

namespace TEKLauncher.Steam;

/// <summary>Manages Steam app's files and configs.</summary>
static class App
{
    /// <summary>Steam installation path.</summary>
    static string s_path = null!;
    /// <summary>Gets a value that indicates whether Steam app is running.</summary>
    public static bool IsRunning
    {
        get
        {
            int? pid = (int?)Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam\ActiveProcess")?.GetValue("pid");
            if (!pid.HasValue || pid.Value == 0)
                return false;
            Process? steamProcess;
            try { steamProcess = Process.GetProcessById(pid.Value); }
            catch (ArgumentException) { steamProcess = null; }
            steamProcess?.Dispose();
            return steamProcess is not null;
        }
    }
    /// <summary>Gets or sets Steam's game installation path if there is one.</summary>
    public static string? GamePath { get; private set; }
    /// <summary>Gets or sets current Steam user status.</summary>
    public static UserStatus CurrentUserStatus { get; private set; }
    /// <summary>Retrieves primary data from Steam config files.</summary>
    public static void Initialize()
    {
        string? path = (string?)Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam")?.GetValue("InstallPath");
        if (path is null)
        {
            Messages.Show("Error", LocManager.GetString(LocCode.SteamMissing));
            Application.Current.Shutdown();
            return;
        }
        s_path = path;
        string configFile = $@"{path}\config\config.vdf";
        if (File.Exists(configFile))
        {
            using var reader = new StreamReader(configFile);
            var vdf = new VDFNode(reader)["Software"]?["Valve"]?["Steam"];
            if (vdf is not null)
            {
                if (uint.TryParse(vdf["CurrentCellID"]?.Value, out uint cellId))
                    CM.Client.CellId = cellId;
                var cmList = vdf["CMWebSocket"];
                if (cmList?.Children is not null)
                {
                    var urls = new Uri[cmList.Children.Count];
                    for (int i = 0; i < urls.Length; i++)
                        urls[i] =  new($"wss://{cmList.Children[i].Key}/cmsocket/");
                    WebSocketConnection.ServerList = new(urls);
                }
            }
        }
        UpdateUserStatus();
    }
    public static void UpdateUserStatus()
    {
        uint steamId3 = (uint)((int?)Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam\ActiveProcess")?.GetValue("ActiveUser") ?? 0);
        if (steamId3 == 0)
        {
            CurrentUserStatus = new(0, Game.Status.NotOwned);
            return;
        }
        Game.Status status = Game.Status.NotOwned;
        string configFile = $@"{s_path}\userdata\{steamId3}\config\localconfig.vdf";
        if (File.Exists(configFile))
        {
            using var reader = new StreamReader(configFile);
            var vdf = new VDFNode(reader)["apptickets"]?["346110"];
            if (vdf is not null)
                status = Game.Status.Owned;
        }
        if (status == Game.Status.Owned)
        {
            string libraryFoldersFile = $@"{s_path}\config\libraryfolders.vdf";
            if (File.Exists(libraryFoldersFile))
            {
                using var reader = new StreamReader(libraryFoldersFile);
                var vdf = new VDFNode(reader);
                if (vdf?.Children is not null)
                    foreach (var library in vdf.Children)
                    {
                        bool gameInstallationFound = false;
                        string? path = library["path"]?.Value;
                        var apps = library["apps"]?.Children;
                        if (path is not null && apps is not null)
                            foreach (var app in apps)
                                if (app.Key == "346110")
                                {
                                    gameInstallationFound = true;
                                    GamePath = $@"{path.Replace(@"\\", @"\")}\steamapps\common\ARK";
                                    if (Game.Path == GamePath)
                                        status = Game.Status.OwnedAndInstalled;
                                    break;
                                }
                        if (gameInstallationFound)
                            break;
                    }
            }
        }
        CurrentUserStatus = new(0x110000100000000ul | steamId3, status);
    }
    /// <summary>Contains user's Steam ID in 64-bit format and their game ownership status.</summary>
    public readonly record struct UserStatus(ulong SteamId64, Game.Status GameStatus);
}