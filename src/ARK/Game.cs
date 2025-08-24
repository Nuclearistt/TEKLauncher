using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using TEKLauncher.Servers;

namespace TEKLauncher.ARK;

/// <summary>Manages game files and parameters.</summary>
static class Game
{
    static bool _dllsDeployed = false;
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
    public static bool RunAsAdmin { get; set; }
    /// <summary>Gets or sets a value that indicates whether game's Steam app ID should be set to 480.</summary>
    public static bool UseSpacewar { get; set; }
    /// <summary>Gets or sets the index of the game localization to use.</summary>
    public static int Language { get; set; } = 4;
    /// <summary>Gets or sets path to ShooterGame.exe.</summary>
    public static string ExePath { get; private set; } = null!;
    /// <summary>Gets or sets path to the root game folder.</summary>
    public static string? Path { get; set; }
    public static void Initialize()
    {
        ExePath = $@"{Path}\ShooterGame\Binaries\Win64\ShooterGame.exe";
    }
    /// <summary>Executes the game and optionally initiates connection to a server.</summary>
    /// <param name="server">Server to join, <see langword="null"/> if no server needs to be joined.</param>
    public static unsafe void Launch(Server? server)
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
            Steam.App.UpdateUserStatus();
            if (Steam.App.CurrentUserStatus.SteamId64 == 0)
            {
                Messages.Show("Error", LocManager.GetString(LocCode.LaunchFailNotLoggedIntoSteam));
                return;
            }
            if (!_dllsDeployed)
			{
				{
					using var resourceStream = Application.GetResourceStream(new("pack://application:,,,/res/libtek-game-runtime.br")).Stream;
					using var decoderStream = new BrotliStream(resourceStream, CompressionMode.Decompress);
					using var fs = File.Create($@"{App.AppDataFolder}\libtek-game-runtime.dll");
					decoderStream.CopyTo(fs);
				}
				{
					using var resourceStream = Application.GetResourceStream(new("pack://application:,,,/res/libtek-injector.br")).Stream;
					using var decoderStream = new BrotliStream(resourceStream, CompressionMode.Decompress);
					using var fs = File.Create($@"{App.AppDataFolder}\libtek-injector.dll");
					decoderStream.CopyTo(fs);
				}
				NativeLibrary.Load($@"{App.AppDataFolder}\libtek-injector.dll");
                _dllsDeployed = true;
			}
            var args = new List<string>(LaunchParameters)
			{
				$"-culture={CultureCodes[Language]}"
			};
            if (server is not null)
            {
                args.Add("+connect");
                args.Add(server.Address);
            }
            var argv = new nint[args.Count];
			for (int i = 0; i < args.Count; i++)
                argv[i] = Marshal.StringToHGlobalUni(args[i]);
			var argvNative = Marshal.AllocHGlobal(argv.Length * Marshal.SizeOf<nint>());
            Marshal.Copy(argv, 0, argvNative, argv.Length);
            TEKInjector.InjFlags flags = 0;
            if (HighProcessPriority)
                flags |= TEKInjector.InjFlags.HighPrio;
            if (RunAsAdmin)
                flags |= TEKInjector.InjFlags.RunAsAdmin;
            var instDlc = new List<uint>();
            foreach (var dlc in DLC.List)
                if (dlc.IsInstalled)
                    instDlc.Add(dlc.AppId);
            var settings = new TekGameRuntimeSettings("steam", 346110, UseSpacewar ? 480u : 0, new Dictionary<uint, string>
            {
                [473850] = "The Center – ARK Expansion Map",
                [508150] = "Primitive+ – ARK Total Conversion",
                [512540] = "ARK: Scorched Earth – Expansion Pack",
                [642250] = "Ragnarok – ARK Expansion Map",
                [696680] = "ARK: Survival Evolved Season Pass",
                [708770] = "ARK: Aberration – Expansion Pack",
                [887380] = "ARK: Extinction – Expansion Pack",
                [1100810] = "Valguero – ARK Expansion Map",
                [1113410] = "ARK: Genesis Season Pass",
                [1270830] = "Crystal Isles – ARK Expansion Map",
                [1691800] = "Lost Island – ARK Expansion Map",
                [1887560] = "Fjordur – ARK Expansion Map",
                [3537070] = "Aquatica – ARK Expansion Map"
            }, [.. instDlc], $@"{Path}\Mods");
			var data = JsonSerializer.SerializeToUtf8Bytes(settings, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
            fixed (byte *dataPtr = data)
			{
				var injArgs = new TEKInjector.Args
				{
					ExePath = Marshal.StringToHGlobalUni(ExePath),
					CurrentDir = 0,
					DllPath = Marshal.StringToHGlobalUni($@"{App.AppDataFolder}\libtek-game-runtime.dll"),
					Type = TEKInjector.LoadType.Pipe,
					Argc = argv.Length,
					Argv = argvNative,
					Flags = flags,
                    DataSize = (uint)data.Length,
                    Data = (nint)dataPtr
				};
                TEKInjector.RunGame(ref injArgs);
                foreach (var ptr in argv)
                    Marshal.FreeHGlobal(ptr);
                Marshal.FreeHGlobal(argvNative);
                Marshal.FreeHGlobal(injArgs.ExePath);
				Marshal.FreeHGlobal(injArgs.DllPath);
				if (injArgs.Result != TEKInjector.Res.Ok)
                {
                    var msg = injArgs.Result switch
                    {
						TEKInjector.Res.GetTokenInfo => "Failed to get process token information",
						TEKInjector.Res.OpenToken => "Failed to open current process token",
						TEKInjector.Res.DuplicateToken => "Failed to duplicate process token",
						TEKInjector.Res.SetTokenInfo => "Failed to set token information",
						TEKInjector.Res.CreateProcess => "Failed to create game process",
						TEKInjector.Res.MemAlloc => "Failed to allocate memory in game process",
						TEKInjector.Res.MemWrite => "Failed to write to game process memory",
						TEKInjector.Res.SecDesc => "Failed to setup security descriptor for the pipe",
						TEKInjector.Res.CreateMapping => "Failed to create file mapping",
						TEKInjector.Res.MapView => "Failed to map view of the file mapping",
						TEKInjector.Res.CreateThread => "Failed to create injection thread",
						TEKInjector.Res.ThreadWait => "Failed to wait for injection thread to finish",
						TEKInjector.Res.DllLoad => "TEK Game Runtime failed to load",
						TEKInjector.Res.ResumeThread => "Failed to resume game's main thread",
						_ => $"Unknown result code {(int)injArgs.Result}"
                    };
                    if (injArgs.Win32Error != 0)
                        msg += $": ({injArgs.Win32Error}) {Marshal.GetPInvokeErrorMessage((int)injArgs.Win32Error)}";
					Messages.Show("Error", msg);
                    return;
				}
			}
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
    readonly record struct TekGameRuntimeSettings(string Store, uint AppId, uint SpoofAppId, Dictionary<uint, string> Dlc, uint[] InstalledDlc, string WorkshopDirPath);
}