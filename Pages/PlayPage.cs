using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using static TEKLauncher.CommunismMode;
using static TEKLauncher.ARK.CreamAPI;
using static TEKLauncher.ARK.Game;
using static TEKLauncher.Data.Settings;
using static TEKLauncher.SteamInterop.Steam;
using static TEKLauncher.UI.Message;

namespace TEKLauncher.Pages
{
    public partial class PlayPage : Page
    {
        public PlayPage()
        {
            InitializeComponent();
            Image.Source = new BitmapImage(new Uri(IsImageAvailable ? ImagePath : "pack://application:,,,/Resources/Images/PlayPage.jpg"));
            RunAsAdminCB.IsChecked = RunAsAdmin;
            PlayCrackedCB.IsChecked = IsSpacewar;
            UseBattlEyeCB.IsChecked = UseBattlEye;
            Localizations.SelectedIndex = GameLang;
        }
        private void InvertPlayCrackedCB() => PlayCrackedCB.IsChecked = !(bool)PlayCrackedCB.IsChecked;
        private void InvokeInvertPlayCrackedCB() => Dispatcher.Invoke(InvertPlayCrackedCB);
        private void LaunchGame(object Sender, RoutedEventArgs Args) => Launch(null, CustomLaunchParameters.Text);
        private void SelectionChangedHandler(object Sender, SelectionChangedEventArgs Args) => GameLang = Localizations.SelectedIndex;
        private void SwitchPlayCracked(object Sender, RoutedEventArgs Args)
        {
            if (IsLoaded)
            {
                if ((bool)PlayCrackedCB.IsChecked)
                {
                    if (!IsInstalled)
                    {
                        if (IsARKPurchased)
                            AppID = 480;
                        else
                        {
                            Show("Warning", "You must install the crack on Settings page in order to play cracked game");
                            new Thread(InvokeInvertPlayCrackedCB).Start();
                        }
                    }
                }
                else
                {
                    if (IsInstalled)
                    {
                        Show("Warning", "You must uninstall the crack on Settings page in order to play legit game");
                        new Thread(InvokeInvertPlayCrackedCB).Start();
                    }
                    else
                        AppID = 346110;
                }
            }
        }
        private void SwitchRunAsAdmin(object Sender, RoutedEventArgs Args)
        {
            if (IsLoaded)
                RunAsAdmin = (bool)RunAsAdminCB.IsChecked;
        }
        private void SwitchUseBattlEye(object Sender, RoutedEventArgs Args)
        {
            if (IsLoaded)
                UseBattlEye = (bool)UseBattlEyeCB.IsChecked;
        }
    }
}