using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using TEKLauncher.ARK;
using TEKLauncher.SteamInterop.Network;
using TEKLauncher.Windows;
using static System.Threading.ThreadPool;
using static System.Windows.Clipboard;
using static System.Windows.Media.Brushes;
using static TEKLauncher.App;
using static TEKLauncher.ARK.ModManager;
using static TEKLauncher.Data.Links;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.SteamInterop.SteamworksAPI;
using static TEKLauncher.UI.Message;
using static TEKLauncher.UI.Notifications;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.Controls
{
    public partial class ModItem : UserControl
    {
        internal ModItem(Mod Mod)
        {
            InitializeComponent();
            this.Mod = Mod;
            bool InfoFileMissing = Mod.Name == string.Empty;
            string NameString = InfoFileMissing ? LocString(LocCode.InfoFileMissing) : Mod.Name;
            ModID.Text = string.Format(LocString(LocCode.ModID), Mod.ID);
            if (Mod.OriginID == 0UL)
            {
                ModName.Text = Mod.Details.Status == 1 ? Mod.Details.Name : Mod.Name;
                if (Mod.Details.Status == 1)
                {
                    ModName.Text = Mod.Details.Name;
                    SecondaryName.Text = NameString;
                    if (InfoFileMissing)
                        SecondaryName.Foreground = DarkRed;
                }
                else
                {
                    ModName.Text = NameString;
                    if (InfoFileMissing)
                        ModName.Foreground = DarkRed;
                }
            }
            else
            {
                OBlock.Visibility = Visibility.Visible;
                OriginalID.Text = string.Format(LocString(LocCode.OriginalID), Mod.OriginID);
                if (Mod.OriginDetails.Status == 1)
                    ModName.Text = Mod.OriginDetails.Name;
                else
                {
                    ModName.Text = NameString;
                    if (InfoFileMissing)
                        ModName.Foreground = DarkRed;
                }
                if (Mod.Details.Status == 1)
                    SecondaryName.Text = Mod.Details.Name;
                else
                {
                    ModName.Text = NameString;
                    if (InfoFileMissing)
                        ModName.Foreground = DarkRed;
                }
            }
            if (ModName.Text.Length > 35)
                ModName.Text = ModName.Text.Substring(0, 35);
            if (SecondaryName.Text.Length > 35)
                SecondaryName.Text = SecondaryName.Text.Substring(0, 35);
            if (Mod.IsInstalled)
            {
                InstalledStatus.Foreground = DarkGreen;
                InstalledStatus.Text = LocString(LocCode.Installed);
            }
            else
            {
                InstalledStatus.Foreground = YellowBrush;
                InstalledStatus.Text = LocString(LocCode.NotInstalled);
                if (!InfoFileMissing)
                    InstallButton.Visibility = Visibility.Visible;
            }
            if (Mod.IsSubscribed == true)
            {
                SubscribedStatus.Foreground = DarkGreen;
                SubscribedStatus.Text = LocString(LocCode.Subscribed);
            }
            else if (Mod.IsSubscribed == false)
            {
                SubscribedStatus.Foreground = YellowBrush;
                SubscribedStatus.Text = LocString(LocCode.NotSubscribed);
                SubscribeButton.Visibility = Visibility.Visible;
            }
            else
            {
                SubscribedStatus.Visibility = Visibility.Collapsed;
            }
            if (Mod.Status != Status.Updating)
            {
                ValidateButton.Visibility = Visibility.Visible;
                if (Mod.UpdateAvailable)
                    UpdateButton.Visibility = Visibility.Visible;
            }
            if (Mod.ImageFile is null || !FileExists(Mod.ImageFile))
                Preview.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/UnknownMod.png"));
            else
            {
                using (FileStream Reader = File.OpenRead(Mod.ImageFile))
                {
                    BitmapImage Image = new BitmapImage();
                    Image.BeginInit();
                    Image.CacheOption = BitmapCacheOption.OnLoad;
                    Image.StreamSource = Reader;
                    try
                    {
                        Image.EndInit();
                        Preview.Source = Image;
                    }
                    catch { Preview.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/UnknownMod.png")); }
                }
            }
        }
        internal readonly Mod Mod;
        private void CopyID(object Sender, RoutedEventArgs Args)
        {
            SetText(Mod.ID.ToString());
            AddImage(LocString(LocCode.ModIDCopied), "Success");
        }
        private void CopyOID(object Sender, RoutedEventArgs Args)
        {
            SetText(Mod.OriginID.ToString());
            AddImage(LocString(LocCode.ModIDCopied), "Success");
        }
        private void Follow(object Sender, RoutedEventArgs Args) => Execute($"{SteamWorkshop}{Mod.ID}");
        private void FollowO(object Sender, RoutedEventArgs Args) => Execute($"{SteamWorkshop}{Mod.OriginID}");
        private void Install(object State)
        {
            bool Failed = false;
            string Name = Mod.OriginDetails.Status == 1 ? Mod.OriginDetails.Name : Mod.OriginID == 0UL && Mod.Details.Status == 1 ? Mod.Details.Name : Mod.Name;
            Notification Notification = Dispatcher.Invoke(() => AddLoading(string.Format(LocString(LocCode.InstallingMod), Name)));
            try { Mod.Install(null, null); }
            catch (Exception Exception)
            {
                Failed = true;
                DeletePath(Mod.ModsPath);
                Dispatcher.Invoke(() => Show("Error", string.Format(LocString(LocCode.ModInstallError), Mod.Name, Exception.Message)));
            }
            Dispatcher.Invoke(Notification.Hide);
            if (!Failed)
            {
                Dispatcher.Invoke(() =>
                {
                    InstalledStatus.Foreground = DarkGreen;
                    InstalledStatus.Text = LocString(LocCode.Installed);
                    Mod.IsInstalled = true;
                });
            }
        }
        private void Install(object Sender, RoutedEventArgs Args)
        {
            InstallButton.Visibility = Visibility.Collapsed;
            QueueUserWorkItem(Install);
        }
        private async void Subscribe(object Sender, RoutedEventArgs Args)
        {
            bool Subscribed = false;
            if (await TryDeployAsync())
                if (await SteamAPI.SubscribeModAsync(Mod.ID))
                    Subscribed = true;
            if (Subscribed)
            {
                SubscribedStatus.Foreground = DarkGreen;
                SubscribedStatus.Text = LocString(LocCode.Subscribed);
                Mod.IsSubscribed = true;
            }
            else
            {
                Show("Info", LocString(LocCode.FailedToSub));
                Execute($"{SteamWorkshop}{Mod.ID}");
            }
            SubscribeButton.Visibility = Visibility.Collapsed;
        }
        private async void Uninstall(object Sender, RoutedEventArgs Args)
        {
            if (Game.IsRunning)
                Show("Warning", LocString(LocCode.CantUninstModGameRunning));
            else if (ShowOptions("Warning", LocString(LocCode.UninstModPrompt)))
            {
                ((Panel)Parent)?.Children?.Remove(this);
                Mod?.Uninstall();
                bool Unsubscribed = false;
                if (await TryDeployAsync() && !(Mod is null))
                    if (await SteamAPI.UnsubscribeModAsync(Mod.ID))
                        Unsubscribed = true;
                if (!Unsubscribed)
                {
                    Execute($"{SteamWorkshop}{Mod.ID}");
                    Show("Info", LocString(LocCode.FailedToUnsub));
                }
                if (!(Mod is null))
                    Mods?.Remove(Mod);
            }
        }
        private void Update(object Sender, RoutedEventArgs Args)
        {
            Mod.Status = Status.Updating;
            ValidateButton.Visibility = UpdateButton.Visibility = Visibility.Collapsed;
            new ValidatorWindow(Mod, false).Show();
        }
        private void Validate(object Sender, RoutedEventArgs Args)
        {
            Mod.Status = Status.Updating;
            ValidateButton.Visibility = UpdateButton.Visibility = Visibility.Collapsed;
            new ValidatorWindow(Mod, true).Show();
        }
    }
}