using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using static System.Windows.Application;
using static TEKLauncher.Data.Links;
using static TEKLauncher.Utils.TEKArchive;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.Pages
{
    public partial class AboutPage : Page
    {
        public AboutPage()
        {
            InitializeComponent();
            string DisplayVersion = App.Version.EndsWith(".0") ? App.Version.Substring(0, App.Version.Length - 2) : App.Version;
            Version.Text = $"ver. {DisplayVersion}";
            using (Stream ResourceStream = GetResourceStream(new Uri($"pack://application:,,,/Resources/About.ta")).Stream)
            using (MemoryStream Stream = new MemoryStream())
            {
                DecompressSingleFile(ResourceStream, Stream);
                Stream.Position = 0L;
                using (StreamReader Reader = new StreamReader(Stream))
                {
                    MainInfo.Text = Reader.ReadLine();
                    Features.Text = Reader.ReadToEnd();
                }
            }
            DownloadLink.Tag = GDriveLauncherFile;
            PatreonLink.Tag = Patreon;
            ArkoudaLink.Tag = DiscordArkouda;
            ARKdictedLink.Tag = DiscordARKdicted;
        }
        private void FollowLink(object Sender, RoutedEventArgs Args) => Execute((string)((FrameworkContentElement)Sender).Tag);
    }
}