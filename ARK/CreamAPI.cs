using System;
using System.IO;
using System.Linq;
using static System.IO.File;
using static System.Windows.Application;
using static TEKLauncher.Utils.TEKArchive;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.ARK
{
    internal static class CreamAPI
    {
        private static readonly string[] CreamAPIFiles = new[] { "cream_api.ini", "steam_api64.dll", "steam_api64_o.dll" };
        internal static bool IsInstalled
        {
            get
            {
                string[] Files = new[]
                {
                    $@"{Game.Path}\Engine\Binaries\ThirdParty\Steamworks\Steamv132\Win64\steam_api64.dll",
                    $@"{Game.Path}\ShooterGame\Binaries\Win64\cream_api.ini",
                    $@"{Game.Path}\ShooterGame\Binaries\Win64\steam_api64_o.dll"
                };
                return Files.All(DLL => FileExists(DLL)) && GetFileSize(Files[0]) != 208296L;
            }
        }
        internal static void Install()
        {
            string[] Directories = new[] { $@"{Game.Path}\Engine\Binaries\ThirdParty\Steamworks\Steamv132\Win64", $@"{Game.Path}\ShooterGame\Binaries\Win64" };
            Directory.CreateDirectory(Directories[0]);
            Directory.CreateDirectory(Directories[1]);
            Game.AppID = 346110;
            foreach (string File in CreamAPIFiles)
                using (Stream ResourceStream = GetResourceStream(new Uri($"pack://application:,,,/Resources/CreamAPI/{File}.ta")).Stream)
                {
                    string FilePath = $@"{Directories[File == "steam_api64.dll" ? 0 : 1]}\{File}";
                    if (Exists(FilePath))
                        SetAttributes(FilePath, GetAttributes(FilePath) & ~FileAttributes.ReadOnly);
                    using (FileStream Writer = Create(FilePath))
                        DecompressSingleFile(ResourceStream, Writer);
                }
        }
        internal static void Uninstall()
        {
            using (Stream ResourceStream = GetResourceStream(new Uri($"pack://application:,,,/Resources/CreamAPI/steam_api64_o.dll.ta")).Stream)
            using (FileStream Writer = Create($@"{Game.Path}\Engine\Binaries\ThirdParty\Steamworks\Steamv132\Win64\steam_api64.dll"))
                DecompressSingleFile(ResourceStream, Writer);
            DeletePath($@"{Game.Path}\ShooterGame\Binaries\Win64\cream_api.ini");
            DeletePath($@"{Game.Path}\ShooterGame\Binaries\Win64\steam_api64_o.dll");
        }
    }
}