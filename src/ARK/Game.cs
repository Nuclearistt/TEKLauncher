using System.Diagnostics;
using System.IO.Compression;
using TEKLauncher.Servers;

namespace TEKLauncher.ARK;

/// <summary>Manages game files and parameters.</summary>
static class Game
{
    /// <summary>Path to version.txt.</summary>
    static string s_versionFilePath = null!;
    /// <summary>Server to join on next game launch.</summary>
    static Server? s_serverToJoin;
    /// <summary>List of codes of all cultures supported by the game.</summary>
    public static readonly string[] CultureCodes = { "ca", "cs", "da", "de", "en", "es", "eu", "fi", "fr", "hu", "it", "ja", "ka", "ko", "nl", "pl", "pt_BR", "ru", "sv", "th", "tr", "uk", "zh", "zh-Hans-CN", "zh-TW" };
    /// <summary>List of standard launch parameters.</summary>
    public static readonly string[] StandardLaunchParameters = { "-usecache", "-useallavailablecores", "-noaafonts", "-sm4", "-d3d10", "-nosplash", "-nomansky", "-nomemorybias", "-lowmemory", "-norhithread", "-novsync", "-preventhibernation", "-allowansel" };
    /// <summary>List of active launch parameters.</summary>
    public static readonly List<string> LaunchParameters = new();
    /// <summary>Gets or sets a value that indicates whether TEK Injector should set game process base priority to high.</summary>
    public static bool HighProcessPriority { get; set; }
    /// <summary>Gets a value that indicates whether game files are corrupted.</summary>
    public static bool IsCorrupted => !File.Exists(ExePath) || !File.Exists(ExePathBE) || !File.Exists($@"{Path}\Engine\Binaries\ThirdParty\Steamworks\Steamv132\Win64\steam_api64.dll");
    /// <summary>Gets a value that indicates whether the game is running.</summary>
    public static bool IsRunning => Process.GetProcessesByName("ShooterGame").Length > 0;
    /// <summary>Gets or sets a value that indicates whether the game should be executed with administrator privileges.</summary>
    public static bool RunAsAdmin { get; set; } = true;
    /// <summary>Gets or sets a value that indicates whether BattlEye should be used.</summary>
    public static bool UseBattlEye { get; set; }
    /// <summary>Gets or sets the index of the game localization to use.</summary>
    public static int Language { get; set; } = 4;
    /// <summary>Gets command line passed to the game process on its creation.</summary>
    public static string CommandLine => $"\"{(UseBattlEye ? ExePathBE : ExePath)}\" {string.Join(' ', LaunchParameters)} -culture={CultureCodes[Language]}{s_serverToJoin?.ConnectionLine}";
    /// <summary>Gets or sets path to ShooterGame.exe.</summary>
    public static string ExePath { get; private set; } = null!;
    /// <summary>Gets or sets path to ShooterGame_BE.exe.</summary>
    public static string ExePathBE { get; private set; } = null!;
    /// <summary>Gets or sets path to the root game folder.</summary>
    public static string? Path { get; set; }
    /// <summary>Gets the game version string.</summary>
    public static string? Version => File.Exists(s_versionFilePath) ? File.ReadAllText(s_versionFilePath).TrimEnd(' ', '\r', '\n') : null;
    /// <summary>Generates auxiliary paths within game folder to be used by the launcher and initializes Steam client folders.</summary>
    public static void Initialize()
    {
        s_versionFilePath = $@"{Path}\version.txt";
        ExePath = $@"{Path}\ShooterGame\Binaries\Win64\ShooterGame.exe";
        ExePathBE = $@"{Path}\ShooterGame\Binaries\Win64\ShooterGame_BE.exe";
        Steam.Client.DownloadsFolder = $@"{Path}\Downloads";
        Steam.Client.ManifestsFolder = $@"{Path}\Manifests";
        try
        {
            Directory.CreateDirectory(Steam.Client.DownloadsFolder).Attributes |= FileAttributes.Hidden;
            Directory.CreateDirectory(Steam.Client.ManifestsFolder).Attributes |= FileAttributes.Hidden;
        }
        catch { }
    }
    /// <summary>Executes the game and optionally initiates connection to a server.</summary>
    /// <param name="server">Server to join, <see langword="null"/> if no server needs to be joined.</param>
    public static void Launch(Server? server)
    {
        if (IsCorrupted)
            Messages.Show("Error", LocManager.GetString(LocCode.LaunchFailCorrupted));
        else if (!Steam.App.IsRunning)
            Messages.Show("Error", LocManager.GetString(LocCode.LaunchFailSteamNotRunning));
        else if (server is not null && server.Map > MapCode.TheIsland && server.Map < MapCode.Mod && !DLC.Get(server.Map).IsInstalled)
            Messages.Show("Warning", LocManager.GetString(LocCode.JoinFailDLCMissing));
        else if (server is not null && float.TryParse(server.Version, out float serverVersion) && float.TryParse(Version, out float clientVersion) && (int)serverVersion != (int)clientVersion)
            Messages.Show("Warning", LocManager.GetString((int)serverVersion > (int)clientVersion ? LocCode.JoinFailServerVersionHigher : LocCode.JoinFailClientVersionHigher));
        else
        {
            Steam.App.UpdateUserStatus();
            if (Steam.App.CurrentUserStatus.SteamId64 == 0)
            {
                Messages.Show("Error", LocManager.GetString(LocCode.LaunchFailNotLoggedIntoSteam));
                return;
            }
            s_serverToJoin = server;
            string tekInjectorPath = $@"{App.AppDataFolder}\TEKInjector.exe";
            using var resourceStream = Application.GetResourceStream(new("pack://application:,,,/res/TEKInjector.br")).Stream;
            using var decoderStream = new BrotliStream(resourceStream, CompressionMode.Decompress);
            using (var writer = File.Create(tekInjectorPath))
                decoderStream.CopyTo(writer);
            var injectorProcess = Process.Start(new ProcessStartInfo { FileName = tekInjectorPath, WorkingDirectory = App.AppDataFolder, CreateNoWindow = true, UseShellExecute = false });
            injectorProcess!.WaitForExit();
            File.Delete(tekInjectorPath);
            int exitCode = injectorProcess.ExitCode;
            if (exitCode != 0)
            {
                int index = exitCode & 0xFFFF;
                var errorType = (TEKInjectorErrorType)((exitCode >> 16) & 0xFFFF);
                Messages.Show("Error", string.Format(LocManager.GetString(LocCode.TekInjectorFailed), $"{errorType}#{index}"));
            }
            if (Settings.CloseOnGameLaunch)
                Application.Current.Shutdown();
        }
    }
    /// <summary>Error types that may be returned by TEK Injector.</summary>
    enum TEKInjectorErrorType
    {
        DriverModuleNotFound = 1,
        FailedToAdjustPrivileges,
        FailedToCreateEvent,
        FailedToCreateFile,
        FailedToCreateProcess,
        FailedToCreateRegistryKey,
        FailedToCreateThread,
        FailedToDuplicateToken,
        FailedToInitializeDriver,
        FailedToLoadDriver,
        FailedToOpenDevice,
        FailedToOpenProcessToken,
        FailedToOpenShare,
        FailedToQuerySystemInformation,
        FailedToQueryThreadInformation,
        FailedToQueryTokenInformation,
        FailedToReadKernelMemory,
        FailedToSetRegistryKeyValue,
        FailedToSetTokenIntegrityLevel,
        FailedToWriteFile,
        FailedToWriteKernelStack,
        FailedToWriteProcessMemory,
        GameProcessNotFound,
        IPCTimedOut,
        KThreadNotFound,
        MemoryAllocationFailed,
        ReturnAddressNotFound,
        ROPGadgetsNotFound,
        ZwCallFailed,
        ZwFunctionsNotFound
    };
    /// <summary>Game ownership status used to initialize ARK Shellcode.</summary>
    public enum Status
    {
        NotOwned,
        Owned,
        OwnedAndInstalled
    }
}