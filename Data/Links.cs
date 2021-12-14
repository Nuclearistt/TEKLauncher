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
                    Reader.ReadLine();
                    Reader.ReadLine();
                    ArkoudaFiles = "http://95.217.84.23/files/Ark/";
                    CA = Reader.ReadLine();
                    CI = Reader.ReadLine();
                    CrashReporterWebhook = Reader.ReadLine();
                    DiscordArkouda = Reader.ReadLine();
                    DiscordARKdicted = Reader.ReadLine();
                    DiscordKillBills = Reader.ReadLine();
                    Reader.ReadLine();
                    DiscordRUSSIA = "https://discord.gg/VpbrQNz";
                    DotNETFramework = Reader.ReadLine();
                    Reader.ReadLine();
                    GDriveBattlEyeFile = Reader.ReadLine();
                    GDriveCommonRedistFile = Reader.ReadLine();
                    GDriveGlobalFontsFile = Reader.ReadLine();
                    GDriveLauncherFile = Reader.ReadLine();
                    GDriveLocalProfileFile = Reader.ReadLine();
                    GDriveVersionFile = Reader.ReadLine();
                    LocalizationFile = Reader.ReadLine();
                    Patreon = Reader.ReadLine();
                    Paypal = Reader.ReadLine();
                    RUSSIA = Reader.ReadLine();
                    SteamWebAPI = Reader.ReadLine();
                    SteamWorkshop = Reader.ReadLine();
                    SupportChannel = Reader.ReadLine();
                }
            }
        }
        internal static readonly string
            ARKdicted,
            ArkoudaFiles,
            CA,
            CI,
            CrashReporterWebhook,
            DiscordArkouda,
            DiscordARKdicted,
            DiscordKillBills,
            DiscordRUSSIA,
            DotNETFramework,
            GDriveBattlEyeFile,
            GDriveCommonRedistFile,
            GDriveGlobalFontsFile,
            GDriveLauncherFile,
            GDriveLocalProfileFile,
            GDriveVersionFile,
            LocalizationFile,
            Patreon,
            Paypal,
            RUSSIA,
            SteamWebAPI,
            SteamWorkshop,
            SupportChannel;
    }
}