using System.Diagnostics;
using System.IO.Compression;
using TEKLauncher.Servers;

namespace TEKLauncher.ARK;

/// <summary>Manages game files and parameters.</summary>
static class Game
{
    /// <summary>Path to version.txt.</summary>
    static string s_versionFilePath = null!;
    /// <summary>List of codes of all cultures supported by the game.</summary>
    public static readonly string[] CultureCodes = { "ca", "cs", "da", "de", "en", "es", "eu", "fi", "fr", "hu", "it", "ja", "ka", "ko", "nl", "pl", "pt_BR", "ru", "sv", "th", "tr", "uk", "zh", "zh-Hans-CN", "zh-TW" };
    /// <summary>List of standard launch parameters.</summary>
    public static readonly string[] StandardLaunchParameters = { "-usecache", "-useallavailablecores", "-high", "-noaafonts", "-sm4", "-d3d10", "-nosplash", "-nomansky", "-nomemorybias", "-lowmemory", "-norhithread", "-novsync", "-preventhibernation", "-allowansel" };
    /// <summary>List of active launch parameters.</summary>
    public static readonly List<string> LaunchParameters = new();
    /// <summary>Gets a value that indicates whether game files are corrupted.</summary>
    public static bool IsCorrupted => !File.Exists(ExePath) || !File.Exists($@"{Path}\Engine\Binaries\ThirdParty\Steamworks\Steamv132\Win64\steam_api64.dll");
    /// <summary>Gets a value that indicates whether the game is running.</summary>
    public static bool IsRunning => Process.GetProcessesByName("ShooterGame").Length > 0;
    /// <summary>Gets or sets a value that indicates whether the game should be executed with administrator privileges.</summary>
    public static bool RunAsAdmin { get; set; } = true;
    /// <summary>Gets or sets a value that indicates whether global fonts should be loaded in the game.</summary>
    public static bool UseGlobalFonts { get; set; }
    /// <summary>Gets or sets a value that indicates whether TEK Injector should be used.</summary>
    public static bool UseTEKInjector { get; set; }
    /// <summary>Gets or sets the index of the game localization to use.</summary>
    public static int Language { get; set; } = 4;
    /// <summary>Gets or sets path to ShooterGame.exe.</summary>
    public static string ExePath { get; private set; } = null!;
    /// <summary>Gets or sets path to the root game folder.</summary>
    public static string? Path { get; set; }
    /// <summary>Gets the game version string.</summary>
    public static string? Version => File.Exists(s_versionFilePath) ? File.ReadAllText(s_versionFilePath).TrimEnd(' ', '\r', '\n') : null;
    /// <summary>Generates auxiliary paths within game folder to be used by the launcher and initializes Steam client folders.</summary>
    public static void Initialize()
    {
        s_versionFilePath = $@"{Path}\version.txt";
        ExePath = $@"{Path}\ShooterGame\Binaries\Win64\ShooterGame.exe";
        Steam.Client.DownloadsFolder = $@"{Path}\Downloads";
        Steam.Client.ManifestsFolder = $@"{Path}\Manifests";
        try
        {
            Directory.CreateDirectory(Steam.Client.DownloadsFolder).Attributes |= FileAttributes.Hidden;
            Directory.CreateDirectory(Steam.Client.ManifestsFolder).Attributes |= FileAttributes.Hidden;
        }
        catch { }
        //The following code is temporary, it's for migration from Cream API to TEK Injector
        string originalDllPath = $@"{Path}\ShooterGame\Binaries\Win64\steam_api64_o.dll";
        if (File.Exists(originalDllPath))
        {
            File.Move(originalDllPath, $@"{Path}\Engine\Binaries\ThirdParty\Steamworks\Steamv132\Win64\steam_api64.dll", true);
            string creamApiIniFile = $@"{Path}\ShooterGame\Binaries\Win64\cream_api.ini";
            if (File.Exists(creamApiIniFile))
                File.Delete(creamApiIniFile);
            UseTEKInjector = true;
        }
    }
    /// <summary>Executes the game and optionally initiates connection to a server.</summary>
    /// <param name="server">Server to join, <see langword="null"/> if no server needs to be joined.</param>
    public static void Launch(Server? server)
    {
        if (IsCorrupted)
            Messages.Show("Error", LocManager.GetString(LocCode.LaunchFailCorrupted));
        else if (!Steam.App.IsRunning)
            Messages.Show("Error", LocManager.GetString(LocCode.LaunchFailSteamNotRunning));
        else if (!UseTEKInjector && !Steam.App.IsARKPurchased)
            Messages.Show("Warning", LocManager.GetString(LocCode.LaunchFailGameNotOwned));
        else if (server is not null && server.Map > MapCode.TheIsland && server.Map < MapCode.Mod && !DLC.Get(server.Map).IsInstalled)
            Messages.Show("Warning", LocManager.GetString(LocCode.JoinFailDLCMissing));
        else if (server is not null && float.TryParse(server.Version, out float serverVersion) && float.TryParse(Version, out float clientVersion) && (int)serverVersion != (int)clientVersion)
            Messages.Show("Warning", LocManager.GetString((int)serverVersion > (int)clientVersion ? LocCode.JoinFailServerVersionHigher : LocCode.JoinFailClientVersionHigher));
        else
        {
            if (UseTEKInjector)
            {
                string tekInjectorPath = $@"{Path}\ShooterGame\Binaries\Win64\TEKInjector.dll";
                if (!File.Exists(tekInjectorPath) || System.Version.TryParse(FileVersionInfo.GetVersionInfo(tekInjectorPath).FileVersion, out var version) && version < new Version(1,0,1,0))
                {
                    using var resourceStream = Application.GetResourceStream(new("pack://application:,,,/res/TEKInjector.br")).Stream;
                    using var decoderStream = new BrotliStream(resourceStream, CompressionMode.Decompress);
                    using var writer = File.Create(tekInjectorPath);
                    decoderStream.CopyTo(writer);
                }
            }
            WinAPI.RunGameProcess($"{string.Join(' ', LaunchParameters)} -culture={(UseGlobalFonts ? "mixed" : CultureCodes[Language])}{server?.ConnectionLine}");
        }
    }
}