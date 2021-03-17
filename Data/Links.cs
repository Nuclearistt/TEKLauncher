using System;
using System.IO;
using static System.Windows.Application;
using static TEKLauncher.Utils.TEKArchive;

namespace TEKLauncher.Data
{
    internal static class Links
    {
        static Links()
        {
            using (Stream ResourceStream = GetResourceStream(new Uri("pack://application:,,,/Resources/Links.ta")).Stream)
            using (MemoryStream Stream = new MemoryStream())
            {
                DecompressSingleFile(ResourceStream, Stream);
                Stream.Position = 0L;
                using (StreamReader Reader = new StreamReader(Stream))
                {
                    ARKdicted = Reader.ReadLine();
                    ArkoudaQuery = Reader.ReadLine();
                    Arkouda2 = Reader.ReadLine();
                    CA = Reader.ReadLine();
                    CI = Reader.ReadLine();
                    CrashReporterWebhook = Reader.ReadLine();
                    DiscordArkouda = Reader.ReadLine();
                    DiscordARKdicted = Reader.ReadLine();
                    DiscordKillBills = Reader.ReadLine();
                    DiscordRUSSIA = Reader.ReadLine();
                    DotNETFramework = Reader.ReadLine();
                    FilesStorage = Reader.ReadLine();
                    GDriveBattlEyeFile = Reader.ReadLine();
                    GDriveCommonRedistFile = Reader.ReadLine();
                    GDriveGlobalFontsFile = Reader.ReadLine();
                    GDriveLauncherFile = Reader.ReadLine();
                    GDriveLocalProfileFile = Reader.ReadLine();
                    GDriveVersionFile = Reader.ReadLine();
                    LocalizationFile = Reader.ReadLine();
                    Patreon = Reader.ReadLine();
                    RUSSIA = Reader.ReadLine();
                    SteamWebAPI = Reader.ReadLine();
                    SteamWorkshop = Reader.ReadLine();
                    SupportChannel = Reader.ReadLine();
                    TrackerWebhook = Reader.ReadLine();
                }
            }
        }
        internal static readonly string
            ARKdicted,
            ArkoudaQuery,
            Arkouda2,
            CA,
            CI,
            CrashReporterWebhook,
            DiscordArkouda,
            DiscordARKdicted,
            DiscordKillBills,
            DiscordRUSSIA,
            DotNETFramework,
            FilesStorage,
            GDriveBattlEyeFile,
            GDriveCommonRedistFile,
            GDriveGlobalFontsFile,
            GDriveLauncherFile,
            GDriveLocalProfileFile,
            GDriveVersionFile,
            LocalizationFile,
            Patreon,
            RUSSIA,
            SteamWebAPI,
            SteamWorkshop,
            SupportChannel,
            TrackerWebhook;
    }
}