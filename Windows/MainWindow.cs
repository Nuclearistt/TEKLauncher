using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TEKLauncher.ARK;
using TEKLauncher.Controls;
using TEKLauncher.Data;
using TEKLauncher.Net;
using TEKLauncher.Pages;
using TEKLauncher.SteamInterop.Network;
using TEKLauncher.UI;
using static System.GC;
using static System.Type;
using static System.IO.File;
using static System.Linq.Expressions.Expression;
using static System.Security.Cryptography.SHA1;
using static System.Windows.Application;
using static System.Windows.SystemParameters;
using static System.Windows.Media.Brushes;
using static TEKLauncher.App;
using static TEKLauncher.CommunismMode;
using static TEKLauncher.ARK.DLCManager;
using static TEKLauncher.ARK.Game;
using static TEKLauncher.Data.Links;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.UI.Message;
using static TEKLauncher.UI.Notifications;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.Windows
{
    public partial class MainWindow : Window
    {
        internal MainWindow()
        {
            MaxHeight = MaximizedPrimaryScreenHeight;
            InitializeComponent();
            if (LocCulture == "ar")
                VersionStack.FlowDirection = FlowDirection.RightToLeft;
            Initialize(this);
            if (!InstallMode)
            {
                if (IsCorrupted)
                    Add(LocString(LocCode.CritFilesMissing), LocString(LocCode.Validate), RunGameValidate);
                Dispatcher.Invoke(CheckForLauncherUpdates);
            }
            Dispatcher.Invoke(CheckForGameAndDLCsUpdates);
        }
        private bool Update = false;
        internal bool ModInstallerMode = false;
        internal ModInstallerPage ModInstallerPage;
        internal SettingsPage SettingsPage;
        private async void CheckForLauncherUpdates()
        {
            string OnlineVersion = await new Downloader().TryDownloadStringAsync($"{Arkouda2}TEKLauncher/Version.txt", $"{FilesStorage}TEKLauncher/Version.txt", GDriveVersionFile);
            if (!(OnlineVersion is null || OnlineVersion == App.Version))
            {
                string DisplayVersion = OnlineVersion.EndsWith(".0") ? OnlineVersion.Substring(0, OnlineVersion.Length - 2) : OnlineVersion;
                Add(string.Format(LocString(LocCode.LUpdateAvailable), DisplayVersion), LocString(LocCode.Update), RunLauncherUpdater);
            }
        }
        private async void CheckForGameAndDLCsUpdates()
        {
            bool QuerySucceeded = false;
            ArkoudaQuery InitialQuery = new ArkoudaQuery();
            if (IsConnectionAvailable())
                for (int AttemptsCounter = 0; !QuerySucceeded && AttemptsCounter < 5; AttemptsCounter++)
                {
                    if (await InitialQuery.RequestAsync())
                        QuerySucceeded = true;
                }
            else
                Message.Show("Warning", LocString(LocCode.NoInternet));
            string Executable = ExecutablePath;
            if (QuerySucceeded && InitialQuery.Checksums.ContainsKey(MapCode.TheIsland))
            {
                if (FileExists(Executable))
                {
                    using (SHA1 SHA = Create())
                    using (FileStream ChecksumStream = OpenRead(Executable))
                    {
                        if (SHA.ComputeHash(ChecksumStream).SequenceEqual(InitialQuery.Checksums[MapCode.TheIsland]))
                            SetCurrentVersion(Game.Version ?? LocString(LocCode.Latest), DarkGreen);
                        else
                        {
                            UpdateAvailable = true;
                            SetCurrentVersion(Game.Version ?? LocString(LocCode.Outdated), YellowBrush);
                            Add(LocString(LocCode.ARKUpdateAvailable), LocString(LocCode.Update), RunGameUpdate);
                        }
                    }
                }
                else
                    SetCurrentVersion(LocString(LocCode.None), DarkRed);
                Notification DLCNotification = AddLoading($"{LocString(LocCode.CheckingForDLCUpds)} ", LocString(LocCode.Update), RunDLCUpdater);
                await CheckForUpdatesAsync(InitialQuery.Checksums);
                bool DLCUpdatesAvailable = false;
                foreach (DLC DLC in DLCs)
                    DLCUpdatesAvailable |= DLC.Status == Status.UpdateAvailable;
                if (DLCUpdatesAvailable)
                    DLCNotification.FinishLoading(LocString(LocCode.DLCUpdsAvailable));
                else
                    DLCNotification.Hide();
            }
            else
            {
                foreach (DLC DLC in DLCs)
                    DLC.SetStatus(DLC.IsInstalled ? Status.Installed : Status.NotInstalled);
                SetCurrentVersion(FileExists(Executable) ? Game.Version ?? LocString(LocCode.NA) : LocString(LocCode.None), (SolidColorBrush)FindResource("GrayBrush"));
            }
        }
        private void Close(object Sender, RoutedEventArgs Args) => Close();
        private void ClosingHandler(object Sender, CancelEventArgs Args)
        {
            ContentDownloader Downloader = SettingsPage?.SteamDownloader;
            if ((Downloader?.IsValidating ?? false) || (Downloader?.IsDownloading ?? false) || (ModInstallerPage?.IsInstalling ?? false) || Current.Windows.OfType<ValidatorWindow>().Any())
            {
                if (ShowOptions("Warning", LocString(LocCode.LauncherClosePrompt)))
                    Current.Shutdown();
                else
                    Args.Cancel = true;
            }
            else if (!Update)
                Current.Shutdown();
        }
        private void Drag(object Sender, MouseButtonEventArgs Args) => DragMove();
        private void LoadedHandler(object Sender, RoutedEventArgs Args)
        {
            if (Settings.CommunismMode)
                Set(true);
        }
        private void Maximize(object Sender, RoutedEventArgs Args) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void Minimize(object Sender, RoutedEventArgs Args) => WindowState = WindowState.Minimized;
        private void NavigatedHandler(object Sender, RoutedEventArgs Args)
        {
            if (!ModInstallerMode && !(ModInstallerPage is null))
                ModInstallerPage = null;
            MenuRadioButton ActiveItem = Menu.ActiveItem;
            if (ActiveItem == Menu.Mods && ModInstallerMode)
                PageFrame.Content = ModInstallerPage ?? (ModInstallerPage = new ModInstallerPage());
            else if (ActiveItem == Menu.Settings)
                PageFrame.Content = SettingsPage ?? (SettingsPage = new SettingsPage());
            else
                PageFrame.Content = Lambda<Func<object>>(New(Type.GetType($"TEKLauncher.Pages.{ActiveItem.Name}Page").GetConstructor(EmptyTypes))).Compile()();
            Collect();
        }
        private void RunDLCUpdater() => Menu.DLCs.IsChecked = true;
        private void RunGameUpdate()
        {
            Menu.Settings.IsChecked = true;
            if (SettingsPage.IsLoaded)
                SettingsPage.Update(false);
            else
                SettingsPage.Loaded += (Sender, Args) => SettingsPage.Update(false);
        }
        private void RunGameValidate()
        {
            Menu.Settings.IsChecked = true;
            if (SettingsPage.IsLoaded)
                SettingsPage.Update(true);
            else
                SettingsPage.Loaded += (Sender, Args) => SettingsPage.Update(true);
        }
        private void RunLauncherUpdater()
        {
            new UpdaterWindow().Show();
            Update = true;
            Close();
        }
        private void StateChangedHandler(object Sender, EventArgs Args)
        {
            if (WindowState == WindowState.Maximized)
            {
                MainBorder.Margin = new Thickness(7D, 7D, 7D, 9D);
                FrameBorder.CornerRadius = MenuBorder.CornerRadius = CaptionBorder.CornerRadius = MainBorder.CornerRadius = new CornerRadius(0D);
                ControlsBlock.MaximizedMode = true;
            }
            else if (WindowState == WindowState.Normal)
            {
                MainBorder.Margin = new Thickness(12.5D);
                MainBorder.CornerRadius = new CornerRadius(25D);
                CaptionBorder.CornerRadius = new CornerRadius(25D, 25D, 0D, 0D);
                ControlsBlock.MaximizedMode = false;
                MenuBorder.CornerRadius = new CornerRadius(0D, 0D, 0D, 25D);
                FrameBorder.CornerRadius = new CornerRadius(0D, 0D, 25D, 0D);
            }
        }
        internal void SetCurrentVersion(string Status, SolidColorBrush Color)
        {
            CurrentVersion.Foreground = Color;
            CurrentVersion.Text = Status;
        }
    }
}