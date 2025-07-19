using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using TEKLauncher.Servers;

namespace TEKLauncher.ARK;

/// <summary>Manages game files and parameters.</summary>
static class Game
{
    /// <summary>Size of <see cref="s_shellcodeImage"/>.</summary>
    static ulong s_shellcodeImageSize;
    /// <summary>ARK Shellcode PE image.</summary>
    static IntPtr s_shellcodeImage;
    /// <summary>ARK Shellcode entry point address.</summary>
    static IntPtr s_shellcodeEntryPoint;
    /// <summary>Path to version.txt.</summary>
    static string s_versionFilePath = null!;
    /// <summary>List of codes of all cultures supported by the game.</summary>
    public static readonly string[] CultureCodes = { "ca", "cs", "da", "de", "en", "es", "eu", "fi", "fr", "hu", "it", "ja", "ka", "ko", "nl", "pl", "pt_BR", "ru", "sv", "th", "tr", "uk", "zh", "zh-Hans-CN", "zh-TW" };
    /// <summary>List of standard launch parameters.</summary>
    public static readonly string[] StandardLaunchParameters = { "-d3d10", "-nosplash", "-nomansky", "-nomemorybias", "-lowmemory", "-norhithread", "-novsync", "-preventhibernation", "-allowansel" };
    /// <summary>List of active launch parameters.</summary>
    public static readonly List<string> LaunchParameters = new();
    /// <summary>Gets a value that indicates whether DirectX is installed.</summary>
    public static bool DirectXInstalled
    {
        get
        {
            string system32 = $@"{Environment.GetFolderPath(Environment.SpecialFolder.Windows)}\System32";
            return File.Exists($@"{system32}\d3d11.dll") && File.Exists($@"{system32}\xinput1_3.dll") && File.Exists($@"{system32}\xapofx1_5.dll") && File.Exists($@"{system32}\x3daudio1_7.dll");
        }
    }
    /// <summary>Gets or sets a value that indicates whether TEK Injector should set game process base priority to high.</summary>
    public static bool HighProcessPriority { get; set; }
    /// <summary>Gets a value that indicates whether game files are corrupted.</summary>
    public static bool IsCorrupted => !File.Exists(ExePath) || !File.Exists($@"{Path}\Engine\Binaries\ThirdParty\Steamworks\Steamv132\Win64\steam_api64.dll");
    /// <summary>Gets a value that indicates whether the game is running.</summary>
    public static bool IsRunning => Process.GetProcessesByName("ShooterGame").Length > 0;
    /// <summary>Gets or sets a value that indicates whether the game should be executed with administrator privileges.</summary>
    public static bool RunAsAdmin { get; set; } = true;
    /// <summary>Gets or sets a value that indicates whether game's Steam app ID should be set to 480.</summary>
    public static bool UseSpacewar { get; set; }
    /// <summary>Gets or sets the index of the game localization to use.</summary>
    public static int Language { get; set; } = 4;
    /// <summary>Gets or sets path to ShooterGame.exe.</summary>
    public static string ExePath { get; private set; } = null!;
    /// <summary>Gets or sets path to the root game folder.</summary>
    public static string? Path { get; set; }
    delegate uint Inject(in InjectionParameters injParams);
    /// <summary>Generates auxiliary paths within game folder to be used by the launcher and initializes Steam client folders.</summary>
    public static void Initialize()
    {
        s_versionFilePath = $@"{Path}\version.txt";
        ExePath = $@"{Path}\ShooterGame\Binaries\Win64\ShooterGame.exe";
    }
    /// <summary>Executes the game and optionally initiates connection to a server.</summary>
    /// <param name="server">Server to join, <see langword="null"/> if no server needs to be joined.</param>
    public static void Launch(Server? server)
    {
        if (IsCorrupted)
            Messages.Show("Error", LocManager.GetString(LocCode.LaunchFailCorrupted));
        else if (!Steam.App.IsRunning)
            Messages.Show("Error", LocManager.GetString(LocCode.LaunchFailSteamNotRunning));
        else if (!DirectXInstalled)
            Messages.Show("Error", string.Format(LocManager.GetString(LocCode.LaunchFailDirectXNotInstalled), LocManager.GetString(LocCode.InstallDirectX)));
        else if (server is not null && server.Map > MapCode.TheIsland && server.Map < MapCode.Mod && !DLC.Get(server.Map).IsInstalled)
            Messages.Show("Warning", LocManager.GetString(LocCode.JoinFailDLCMissing));
        else
        {
            using (var reader = File.OpenRead($@"{Path}\Engine\Binaries\ThirdParty\Steamworks\Steamv132\Win64\steam_api64.dll"))
                if (CRC32.ComputeHash(reader) != 0xC56B2718)
                {
                    Messages.Show("Error", LocManager.GetString(LocCode.LaunchFailSteamApiCompromised));
                    return;
                }
            Steam.App.UpdateUserStatus();
            if (Steam.App.CurrentUserStatus.SteamId64 == 0)
            {
                Messages.Show("Error", LocManager.GetString(LocCode.LaunchFailNotLoggedIntoSteam));
                return;
            }
            if (s_shellcodeImage == IntPtr.Zero)
            {
                using var resourceStream = Application.GetResourceStream(new("pack://application:,,,/res/ARKShellcode.br")).Stream;
                using var decoderStream = new BrotliStream(resourceStream, CompressionMode.Decompress);
                using var ms = new MemoryStream((int)resourceStream.Length * 2);
                decoderStream.CopyTo(ms);
                s_shellcodeImage = WinAPI.LoadPeImage(ms.ToArray(), out s_shellcodeEntryPoint, out s_shellcodeImageSize);
            }
            string modsDirectorySearchPath = $@"{Path}\Mods\*";
            Status status = Steam.App.CurrentUserStatus.GameStatus;
            if (status != Status.NotOwned && UseSpacewar)
                status = Status.NotOwned;
            uint exitCode = Marshal.GetDelegateForFunctionPointer<Inject>(s_shellcodeEntryPoint)(new()
            {
                ImageBase = s_shellcodeImage,
                ImageSize = s_shellcodeImageSize,
                ExePath = ExePath,
                CommandLine = $"\"{ExePath}\" {string.Join(' ', LaunchParameters)} -culture={CultureCodes[Language]}{server?.ConnectionLine}",
                CurrentDirectory = $@"{Path}\ShooterGame\Binaries\Win64",
                SteamApiPath = $@"{Path}\Engine\Binaries\ThirdParty\Steamworks\Steamv132\Win64\steam_api64.dll",
                ModsDirectorySearchPath = modsDirectorySearchPath,
                ModsDirectorySearchPathSize = (ulong)modsDirectorySearchPath.Length * 2,
                Status = status,
                SteamId = Steam.App.CurrentUserStatus.SteamId64,
                ReduceIntegrityLevel = !RunAsAdmin,
                SetHighProcessPriority = HighProcessPriority
            });
            if (exitCode > 0)
                Messages.Show("Error", string.Format(LocManager.GetString(LocCode.InjectionFailed), exitCode));
            if (Settings.CloseOnGameLaunch)
                Application.Current.Shutdown();
        }
    }
    /// <summary>Game ownership status used to initialize ARK Shellcode.</summary>
    public enum Status
    {
        NotOwned,
        Owned,
        OwnedAndInstalled
    }
    struct InjectionParameters
    {
        public IntPtr ImageBase;
        public ulong ImageSize;
        [MarshalAs(UnmanagedType.LPWStr)] public string ExePath;
        [MarshalAs(UnmanagedType.LPWStr)] public string CommandLine;
        [MarshalAs(UnmanagedType.LPWStr)] public string CurrentDirectory;
        [MarshalAs(UnmanagedType.LPWStr)] public string SteamApiPath;
        [MarshalAs(UnmanagedType.LPWStr)] public string ModsDirectorySearchPath;
        public ulong ModsDirectorySearchPathSize;
        public Status Status;
        public ulong SteamId;
        public bool ReduceIntegrityLevel;
        public bool SetHighProcessPriority;
    }
}