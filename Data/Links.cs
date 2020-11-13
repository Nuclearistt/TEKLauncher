using System;
using System.IO;
using System.Net;
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
                    CA = Reader.ReadLine();
                    CI = Reader.ReadLine();
                    CrashReporterWebhook = Reader.ReadLine();
                    DiscordArkouda = Reader.ReadLine();
                    DiscordARKdicted = Reader.ReadLine();
                    DiscordKillBills = Reader.ReadLine();
                    DiscordARKRussia = Reader.ReadLine();
                    DotNETFramework = Reader.ReadLine();
                    GDriveBattlEyeFile = Reader.ReadLine();
                    GDriveCommonRedistFile = Reader.ReadLine();
                    GDriveLauncherFile = Reader.ReadLine();
                    GDriveLocalProfileFile = Reader.ReadLine();
                    GDriveVersionFile = Reader.ReadLine();
                    Seedbox = Reader.ReadLine();
                    SteamWebAPI = Reader.ReadLine();
                    SupportChannel = Reader.ReadLine();
                    TrackerWebhook = Reader.ReadLine();
                    SeedboxCredential = new NetworkCredential(Reader.ReadLine(), Reader.ReadLine());
                }
            }
        }
        internal static readonly string
            ARKdicted,
            ArkoudaQuery,
            CA,
            CI,
            CrashReporterWebhook,
            DiscordArkouda,
            DiscordARKdicted,
            DiscordKillBills,
            DiscordARKRussia,
            DotNETFramework,
            GDriveBattlEyeFile,
            GDriveCommonRedistFile,
            GDriveLauncherFile,
            GDriveLocalProfileFile,
            GDriveVersionFile,
            Seedbox,
            SteamWebAPI,
            SupportChannel,
            TrackerWebhook;
        internal static readonly NetworkCredential SeedboxCredential;
    }
}