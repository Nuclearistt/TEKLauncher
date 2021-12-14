using System.Windows;
using System.Windows.Controls;
using static TEKLauncher.Data.Links;
using static TEKLauncher.Data.LocalizationManager;
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
            MainInfo.Text = LocString(LocCode.MainInfo);
            Features.Text = LocString(LocCode.AboutFeatures);
            DownloadLink.Tag = "https://github.com/Nuclearistt/TEKLauncher/releases/latest/download/TEKLauncher.exe";
            LocLink.Tag = LocalizationFile;
            PatreonLink.Tag = Patreon;
            PaypalLink.Tag = Paypal;
            ArkoudaLink.Tag = DiscordArkouda;
            ARKdictedLink.Tag = DiscordARKdicted;
        }
        private void FollowLink(object Sender, RoutedEventArgs Args) => Execute((string)((FrameworkContentElement)Sender).Tag);
    }
}