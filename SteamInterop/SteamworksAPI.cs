using System;
using System.IO;
using System.Threading.Tasks;
using TEKLauncher.SteamInterop.Steamworks;
using static System.Environment;
using static System.IO.File;
using static System.Threading.Tasks.Task;
using static System.Windows.Application;
using static TEKLauncher.App;
using static TEKLauncher.SteamInterop.Steam;
using static TEKLauncher.Utils.TEKArchive;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.SteamInterop
{
    internal static class SteamworksAPI
    {
        private static readonly string AppIDFile = $@"{CurrentDirectory}\steam_appid.txt", SteamAPIFile = $@"{AppDataFolder}\steam_api64.dll";
        private static readonly object Lock = new object();
        internal static readonly SteamAPI SteamAPI = new SteamAPI();
        private static void Deploy()
        {
            try
            {
                string WindowsSteamAPIFile = $@"{GetFolderPath(SpecialFolder.Windows) ?? @"C:\Windows"}\System32\steam_api64.dll";
                if (FileExists("steam_api64.dll"))
                    Delete("steam_api64.dll");
                if (FileExists(WindowsSteamAPIFile))
                    Delete(WindowsSteamAPIFile);
                using (Stream ResourceStream = GetResourceStream(new Uri("pack://application:,,,/Resources/steam_api64.dll.ta")).Stream)
                using (FileStream Writer = Create(SteamAPIFile))
                    DecompressSingleFile(ResourceStream, Writer);
                WriteAllText(AppIDFile, "480");
                SteamAPI?.Load();
                Delete(AppIDFile);
            }
            catch { }
        }
        internal static void Retract()
        {
            if (SteamAPI.IsLoaded)
                SteamAPI.Unload();
            if (FileExists(SteamAPIFile))
                Delete(SteamAPIFile);
        }
        internal static bool TryDeploy()
        {
            lock (Lock)
            {
                if (!IsRunning)
                    return false;
                if (SteamAPI?.IsLoaded ?? false)
                    return true;
                Deploy();
                return SteamAPI?.IsLoaded ?? false;
            }
        }
        internal static Task<bool> TryDeployAsync() => Run(TryDeploy);
    }
}