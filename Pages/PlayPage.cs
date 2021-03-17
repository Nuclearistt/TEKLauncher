using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using TEKLauncher.ARK;
using TEKLauncher.Data;
using static System.Array;
using static System.IO.File;
using static System.Windows.Application;
using static TEKLauncher.CommunismMode;
using static TEKLauncher.ARK.CreamAPI;
using static TEKLauncher.ARK.Game;
using static TEKLauncher.ARK.LaunchParameters;
using static TEKLauncher.Data.LocalizationManager;
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
            if (LocCulture == "el")
            {
                UseBattlEyeCB.FontSize = UseSpacewarCB.FontSize = RunAsAdminCB.FontSize = 18D;
                GameLocTB.FontSize = 20D;
            }
            else if (LocCulture == "ar")
                GameLangStack.FlowDirection = LangStack.FlowDirection = FlowDirection.RightToLeft;
            Image.Source = new BitmapImage(new Uri(IsImageAvailable ? ImagePath : "pack://application:,,,/Resources/Images/PlayPage.jpg"));
            RunAsAdminCB.IsChecked = RunAsAdmin;
            UseSpacewarCB.IsChecked = IsSpacewar;
            UseBattlEyeCB.IsChecked = UseBattlEye;
            Languages.SelectedIndex = IndexOf(SupportedCultures, LocCulture);
            Languages.SelectionChanged += SelectionChangedHandler;
            Localizations.SelectedIndex = GameLang;
            Localizations.SelectionChanged += SelectionChangedHandler;
            CustomLaunchParameters.Text = Settings.CustomLaunchParameters;
        }
        private void InvertUseSpacewarCB() => UseSpacewarCB.IsChecked = !(bool)UseSpacewarCB.IsChecked;
        private void InvokeInvertUseSpacewarCB() => Dispatcher.Invoke(InvertUseSpacewarCB);
        private void LaunchGame(object Sender, RoutedEventArgs Args) => Launch(null, CustomLaunchParameters.Text);
        private void SelectionChangedHandler(object Sender, SelectionChangedEventArgs Args)
        {
            if (Sender == Languages)
            {
                if (ShowOptions("Warning", LocString(LocCode.SwitchLangPrompt)))
                {
                    Lang = SupportedCultures[Languages.SelectedIndex];
                    Current.Shutdown();
                }
                else
                {
                    Languages.SelectionChanged -= SelectionChangedHandler;
                    Languages.SelectedIndex = IndexOf(SupportedCultures, LocCulture);
                    Languages.SelectionChanged += SelectionChangedHandler;
                }
            }
            else
            {
                int LocIndex = GameLang = Localizations.SelectedIndex;
                if (GlobalFontsInstalled)
                {
                    string GlobalFolder = $@"{Game.Path}\ShooterGame\Content\Localization\Game\global", NewLocFolder = $@"{Game.Path}\ShooterGame\Content\Localization\Game\{GameCultureCodes[LocIndex]}";
                    if (Directory.Exists(NewLocFolder))
                    {
                        Copy($@"{NewLocFolder}\ShooterGame.archive", $@"{GlobalFolder}\ShooterGame.archive", true);
                        Copy($@"{NewLocFolder}\ShooterGame.locres", $@"{GlobalFolder}\ShooterGame.locres", true);
                    }
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
        private void SwitchUseSpacewar(object Sender, RoutedEventArgs Args)
        {
            if (IsLoaded)
            {
                if ((bool)UseSpacewarCB.IsChecked)
                {
                    if (!IsInstalled)
                    {
                        if (IsARKPurchased)
                            AppID = 480;
                        else
                        {
                            Show("Warning", LocString(LocCode.NeedCreamAPI));
                            new Thread(InvokeInvertUseSpacewarCB).Start();
                        }
                    }
                }
                else
                {
                    if (IsInstalled)
                    {
                        Show("Warning", LocString(LocCode.NeedNoCreamAPI));
                        new Thread(InvokeInvertUseSpacewarCB).Start();
                    }
                    else
                        AppID = 346110;
                }
            }
        }
        private void TextChangedHandler(object Sender, TextChangedEventArgs Args) => Settings.CustomLaunchParameters = CustomLaunchParameters.Text;
    }
}