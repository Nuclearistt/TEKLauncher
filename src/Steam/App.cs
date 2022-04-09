using System.Diagnostics;
using Microsoft.Win32;
using TEKLauncher.Steam.CM;

namespace TEKLauncher.Steam;

/// <summary>Manages Steam app's files and configs.</summary>
static class App
{
    /// <summary>Gets a value that indicates whether ARK is purchased on current Steam account.</summary>
    public static bool IsARKPurchased { get; private set; }
    /// <summary>Gets a value that indicates whether Steam app is running.</summary>
    public static bool IsRunning
    {
        get
        {
            int? pid = (int?)Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam")?.GetValue("SteamPID");
            if (!pid.HasValue)
                return false;
            Process? steamProcess;
            try { steamProcess = Process.GetProcessById(pid.Value); }
            catch (ArgumentException) { steamProcess = null; }
            steamProcess?.Dispose();
            return steamProcess is not null;
        }
    }
    /// <summary>Gets path to Steam installation folder.</summary>
    public static string Path { get; private set; } = null!;
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
        Path = path; 
        string configFile = $@"{Path}\config\config.vdf";
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
        IsARKPurchased = (int?)Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam\Apps\346110")?.GetValue("Installed") == 1;
    }
}