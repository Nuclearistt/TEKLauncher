using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using TEKLauncher.Pages;
using TEKLauncher.SteamInterop.Network.CM.Messages.Bodies;
using static System.Windows.Clipboard;
using static TEKLauncher.App;
using static TEKLauncher.ARK.ModManager;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.UI.Notifications;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.Controls
{
    public partial class WBItem : UserControl
    {
        internal WBItem(ItemDetails Details)
        {
            InitializeComponent();
            ModName.Text = Details.Name;
            ModID.Text = string.Format(LocString(LocCode.ModID), ID = Details.ID.ToString());
            try { Preview.Source = new BitmapImage(new Uri(Details.PreviewURL)); }
            catch { Preview.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/UnknownMod.png")); }
            if (!(Mods.Find(Mod => Mod.OriginID == Details.ID) is null))
                InstallButton.Content = LocString(LocCode.Installed);
        }
        private readonly string ID;
        private void CopyID(object Sender, RoutedEventArgs Args)
        {
            SetText(ID);
            AddImage(LocString(LocCode.ModIDCopied), "Success");
        }
        private void Follow(object Sender, RoutedEventArgs Args) => Execute($"steam://openurl/https://steamcommunity.com/sharedfiles/filedetails/?id={ID}");
        private void Install(object Sender, RoutedEventArgs Args) => Instance.MWindow.PageFrame.Content = new ModInstallerPage(ID);
    }
}