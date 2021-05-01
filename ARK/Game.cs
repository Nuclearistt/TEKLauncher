using System.IO;
using System.Linq;
using TEKLauncher.Data;
using TEKLauncher.Servers;
using TEKLauncher.SteamInterop;
using static System.IO.File;
using static System.Diagnostics.Process;
using static TEKLauncher.App;
using static TEKLauncher.ARK.CreamAPI;
using static TEKLauncher.ARK.LaunchParameters;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.Data.Settings;
using static TEKLauncher.SteamInterop.Steam;
using static TEKLauncher.UI.Message;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.ARK
{
    internal static class Game
    {
        internal static bool IsCorrupted = false, UpdateAvailable = false;
        internal static string Path;
        internal static bool GlobalFontsInstalled
        {
            get
            {
                string GlobalFolder = $@"{Path}\ShooterGame\Content\Localization\Game\global";
                return Directory.Exists(GlobalFolder) && Directory.EnumerateFiles(GlobalFolder).Count() != 0;
            }
        }
        internal static bool IsRunning => GetProcessesByName("ShooterGame").Length != 0;
        internal static bool IsSpacewar => IsInstalled || AppID == 480;
        internal static int AppID
        {
            get
            {
                string AppIDFile = $@"{Path}\ShooterGame\Binaries\Win64\steam_appid.txt";
                return FileExists(AppIDFile) ? int.TryParse(ReadAllText(AppIDFile), out int ID) ? ID : 0 : 0;
            }
            set
            {
                Directory.CreateDirectory($@"{Path}\ShooterGame\Binaries\Win64");
                string FilePath = $@"{Path}\ShooterGame\Binaries\Win64\steam_appid.txt";
                SetAttributes(FilePath, GetAttributes(FilePath) & ~FileAttributes.ReadOnly);
                WriteAllText(FilePath, value.ToString());
            }
        }
        internal static string BEExecutablePath => $@"{Path}\ShooterGame\Binaries\Win64\ShooterGame_BE.exe";
        internal static string ExecutablePath => $@"{Path}\ShooterGame\Binaries\Win64\ShooterGame.exe";
        internal static string Version
        {
            get
            {
                string VersionPath = $@"{Path}\version.txt";
                return FileExists(VersionPath) ? ReadAllText(VersionPath).TrimEnd(' ', '\r', '\n') : null;
            }
        }
        internal static void Initialize()
        {
            Path = ARKPath;
            if (FileExists(ExecutablePath) && FileExists(BEExecutablePath) && FileExists($@"{Path}\Engine\Binaries\ThirdParty\Steamworks\Steamv132\Win64\steam_api64.dll"))
            {
                if (IsInstalled && !IsRunning && GetProcessesByName("ShooterGameServer").Length == 0)
                    Install();
            }
            else
                IsCorrupted = true;
        }
        internal static void Launch(Server Server, string CustomLaunchParameters = null)
        {
            if (!(FileExists(ExecutablePath) && FileExists(BEExecutablePath)))
                Show("Warning", LocString(LocCode.CantLaunchExeMissing));
            else if (IsCorrupted)
                Show("Warning", LocString(LocCode.CantLaunchCorruped));
            else if (!Steam.IsRunning)
                Show("Warning", LocString(LocCode.CantLaunchNoSteam));
            else if (!IsInstalled && !IsARKPurchased)
                Show("Warning", LocString(LocCode.CantLaunchNoCreamAPI));
            else if (!(Server is null || IsConnectionAvailable()))
                Show("Warning", LocString(LocCode.CantJoinNoInternet));
            else if (IsSpacewar && !IsSpacewarInstalled)
                Show("Warning", LocString(LocCode.CantLaunchNoSpacewar));
            else
            {
                string ParametersLine = $"{Settings.LaunchParameters} {Server?.ConnectionLine} {CustomLaunchParameters} {CultureParameter}".Trim().Replace("   ", " ").Replace("  ", " ");
                if (IsSpacewar)
                    Execute(UseBattlEye ? BEExecutablePath : ExecutablePath, ParametersLine);
                else
                {
                    Steam.LaunchParameters = ParametersLine;
                    Execute($@"{Steam.Path}\Steam.exe", "-applaunch 346110");
                    Steam.LaunchParameters = Settings.LaunchParameters;
                }
                if (CloseOnGameRun)
                    Instance.MWindow.Close();
            }
        }
    }
}