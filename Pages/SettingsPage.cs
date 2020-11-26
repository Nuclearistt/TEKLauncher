using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shell;
using TEKLauncher.ARK;
using TEKLauncher.Data;
using TEKLauncher.Controls;
using TEKLauncher.Net;
using TEKLauncher.SteamInterop.Network;
using TEKLauncher.Windows;
using static System.Environment;
using static System.Diagnostics.Process;
using static System.IO.Directory;
using static System.Windows.Application;
using static System.Windows.Media.Brushes;
using static TEKLauncher.App;
using static TEKLauncher.Data.Links;
using static TEKLauncher.UI.Message;
using static TEKLauncher.UI.Notifications;
using static TEKLauncher.Utils.TEKArchive;
using static TEKLauncher.Utils.UtilFunctions;
using TEKLauncher.SteamInterop;

namespace TEKLauncher.Pages
{
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
            foreach (Panel Stack in LPGrid.Children)
                foreach (CheckBox Checkbox in Stack.Children)
                {
                    Checkbox.IsChecked = LaunchParameters.ParameterExists((string)Checkbox.Tag);
                    Checkbox.Checked += CheckParameter;
                    Checkbox.Unchecked += UncheckParameter;
                }
            ProgressBar.ProgressUpdated = ProgressUpdatedHandler;
            Downloader = new Downloader(ProgressBar.Progress) { DownloadBegan = DownloadBeganHandler };
            SteamDownloader = new ContentDownloader(346111U, FinishHandler, SetStatus, ProgressBar);
        }
        private string InstallingItem;
        private readonly Downloader Downloader;
        internal readonly ContentDownloader SteamDownloader;
        private void CheckParameter(object Sender, RoutedEventArgs Args) => LaunchParameters.Add((string)((FrameworkElement)Sender).Tag);
        private void DownloadBeganHandler() => SetStatus($"Downloading {InstallingItem}", YellowBrush);
        private void FailedToDownload()
        {
            SetStatus($"Failed to download {InstallingItem}. Try again later", DarkRed);
            SwitchButtons(true);
        }
        private void FinishHandler()
        {
            SwitchButtons(true, true);
            ValidationBlock.Visibility = Visibility.Collapsed;
        }
        private async void FixBattlEye(object Sender, RoutedEventArgs Args)
        {
            if (Game.IsRunning)
                AddImage("Can't fix BattlEye while game is running!", "Error");
            else if (!FileExists(Game.BEExecutablePath))
                AddImage("Can't fix BattlEye because ShooterGame_BE.exe is missing", "Error");
            else
            {
                InstallingItem = "BattlEye";
                PreparingToDownload();
                string ArchivePath = $@"{AppDataFolder}\BattlEye.ta";
                ProgressBar.SetDownloadMode();
                if (await Downloader.TryDownloadFileAsync(ArchivePath, $"{Seedbox}Extra/BattlEye.ta", GDriveBattlEyeFile))
                {
                    Start(Game.BEExecutablePath, "4 0").WaitForExit();
                    DeletePath($@"{Game.Path}\ShooterGame\Binaries\Win64\BattlEye");
                    DeletePath($@"{GetFolderPath(SpecialFolder.ProgramFilesX86)}\Common Files\BattlEye\BEService_ark.exe");
                    DeletePath($@"{GetFolderPath(SpecialFolder.LocalApplicationData)}\BattlEye\ark");
                    SetStatus("Extracting archive", YellowBrush);
                    if (await DecompressArchiveAsync(ArchivePath, $@"{Game.Path}\ShooterGame\Binaries\Win64\BattlEye"))
                    {
                        File.Delete(ArchivePath);
                        Start(Game.BEExecutablePath, "1 0").WaitForExit();
                        SetStatus("Successfully reinstalled BattlEye", DarkGreen);
                    }
                    else
                        SetStatus("Failed to extract archive, try again", DarkRed);
                    SwitchButtons(true);
                }
                else
                    FailedToDownload();
                Instance.MWindow.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
            }
        }
        private void FixBloom(object Sender, RoutedEventArgs Args)
        {
            if (Game.IsRunning)
            {
                AddImage("Can't fix bloom while game is running!", "Error");
                return;
            }
            string BaseScalabilityIni = $@"{Game.Path}\Engine\Config\BaseScalability.ini";
            if (FileExists(BaseScalabilityIni))
            {
                string[] Lines = File.ReadAllLines(BaseScalabilityIni);
                for (int Iterator = 0; Iterator < Lines.Length; Iterator++)
                    if (Lines[Iterator].StartsWith("r.Bloom"))
                        Lines[Iterator] = "r.BloomQuality=1";
                File.WriteAllLines(BaseScalabilityIni, Lines);
            }
            AddImage("Bloom has been fixed successfully", "Success");
        }
        private async void InstallRequirements(object Sender, RoutedEventArgs Args)
        {
            InstallingItem = "requirements";
            PreparingToDownload();
            string ArchivePath = $@"{AppDataFolder}\CommonRedist.ta";
            ProgressBar.SetDownloadMode();
            if (await Downloader.TryDownloadFileAsync(ArchivePath, $"{Seedbox}Extra/CommonRedist.ta", GDriveCommonRedistFile))
            {
                string CommonRedistFolder = $@"{AppDataFolder}\CommonRedist";
                SetStatus("Extracting archive", YellowBrush);
                if (await DecompressArchiveAsync(ArchivePath, CommonRedistFolder))
                {
                    File.Delete(ArchivePath);
                    SetStatus("Installing requirements", YellowBrush);
                    Start($@"{CommonRedistFolder}\DirectX\DXSETUP.exe", "/silent").WaitForExit();
                    Start($@"{CommonRedistFolder}\MSVCP\2010\vcredist_x64.exe", "/q /norestart").WaitForExit();
                    Start($@"{CommonRedistFolder}\MSVCP\2010\vcredist_x86.exe", "/q /norestart").WaitForExit();
                    Start($@"{CommonRedistFolder}\MSVCP\2012\vcredist_x64.exe", "/install /quiet /norestart").WaitForExit();
                    Start($@"{CommonRedistFolder}\MSVCP\2012\vcredist_x86.exe", "/install /quiet /norestart").WaitForExit();
                    Start($@"{CommonRedistFolder}\MSVCP\2013\vcredist_x64.exe", "/install /quiet /norestart").WaitForExit();
                    Start($@"{CommonRedistFolder}\MSVCP\2013\vcredist_x86.exe", "/install /quiet /norestart").WaitForExit();
                    DeleteDirectory(CommonRedistFolder);
                    SetStatus("Successfully installed requirements", DarkGreen);
                }
                else
                    SetStatus("Failed to extract archive", DarkRed);
                SwitchButtons(true);
            }
            else
                FailedToDownload();
            Instance.MWindow.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
        }
        private void InstallSpacewar(object Sender, RoutedEventArgs Args)
        {
            if (Steam.IsSpacewarInstalled)
                AddImage("You have Spacewar installed already", "Success");
            else if (!Steam.IsRunning)
                AddImage("Steam must be running to install Spacewar", "Error");
            else
                Execute("steam://install/480");
        }
        private void LoadedHandler(object Sender, RoutedEventArgs Args)
        {
            UpdateCrackButtons();
            if (InstallMode)
            {
                Update(false);
                Show("Info", "Game download has been initiated, wait for it to finish, if you'll close launcher, press \"Validate\" next time you open it to continue");
            }
        }
        private void PreparingToDownload()
        {
            SwitchButtons(false);
            SetStatus($"Preparing to download {InstallingItem}...", YellowBrush);
        }
        private void ProgressUpdatedHandler()
        {
            Instance.MWindow.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            Instance.MWindow.TaskbarItemInfo.ProgressValue = ProgressBar.Progress.Ratio;
            if (SteamDownloader.IsValidating)
            {
                if (ValidationBlock.Visibility == Visibility.Collapsed)
                    ValidationBlock.Visibility = Visibility.Visible;
                FilesMissing.Text = SteamDownloader.FilesMissing.ToString();
                FilesOutdated.Text = SteamDownloader.FilesOutdated.ToString();
                FilesUpToDate.Text = SteamDownloader.FilesUpToDate.ToString();
            }
        }
        private void ReapplyCrack(object Sender, RoutedEventArgs Args)
        {
            if (Game.IsRunning)
                AddImage("Can't reapply crack while game is running!", "Error");
            else
            {
                CreamAPI.Install();
                AddImage("Successfully reapplied crack", "Success");
            }
        }
        private void SetStatus(string Text, SolidColorBrush Color)
        {
            Status.Foreground = Color;
            Status.Text = Text;
        }
        private void SwitchButtons(bool State, bool Update = false)
        {
            InstallReqButton.IsEnabled = ValidateButton.IsEnabled = UnlockButton.IsEnabled = FixBEButton.IsEnabled = State;
            if (Update)
                UpdateButton.Content = ((VectorImage)UpdateButton.Template.FindName("Icon", UpdateButton)).Source = State ? "Update" : "Pause";
            else
                UpdateButton.IsEnabled = State;
        }
        private void SwitchCrack(object Sender, RoutedEventArgs Args)
        {
            bool CrackInstalled = CreamAPI.IsInstalled;
            string Prefix = CrackInstalled ? "un" : null;
            if (Game.IsRunning)
                AddImage($"Can't {Prefix}install crack while game is running!", "Error");
            else
            {
                if (CrackInstalled)
                    CreamAPI.Uninstall();
                else
                    CreamAPI.Install();
                UpdateCrackButtons();
                AddImage($"Successfully {Prefix}installed crack", "Success");
            }
        }
        private void UncheckParameter(object Sender, RoutedEventArgs Args) => LaunchParameters.Remove((string)((FrameworkElement)Sender).Tag);
        private void UninstallAllMods(object Sender, RoutedEventArgs Args)
        {
            if (!Instance.Windows.OfType<ModUninstallerWindow>().Any())
                new ModUninstallerWindow().Show();
        }
        private async void UnlockSkins(object Sender, RoutedEventArgs Args)
        {
            if (Game.IsRunning)
                SetStatus("Can't unlock all skins while game is running!", DarkRed);
            else
            {
                string LocalProfiles = $@"{Game.Path}\ShooterGame\Saved\LocalProfiles";
                CreateDirectory(LocalProfiles);
                InstallingItem = "local profile";
                PreparingToDownload();
                ProgressBar.SetDownloadMode();
                if (await Downloader.TryDownloadFileAsync($@"{LocalProfiles}\PlayerLocalData.arkprofile", $"{Seedbox}Extra/PlayerLocalData.arkprofile", GDriveLocalProfileFile))
                {
                    SetStatus("Successfully unlocked all skins, haircuts and maps", DarkGreen);
                    SwitchButtons(true);
                }
                else
                    FailedToDownload();
                Instance.MWindow.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
            }
        }
        private void Update(object Sender, RoutedEventArgs Args)
        {
            if ((string)UpdateButton.Content == "Update")
                Update(false);
            else
                SteamDownloader.Pause();
        }
        private void UpdateCrackButtons()
        {
            bool CrackInstalled = CreamAPI.IsInstalled;
            SwitchCrackButton.Content = CrackInstalled ? "Uninstall" : "Install";
            ((VectorImage)SwitchCrackButton.Template.FindName("Icon", SwitchCrackButton)).Source = CrackInstalled ? "Delete" : "Install";
            ReapplyCrackButton.IsEnabled = CrackInstalled;
        }
        private void UpdateJob(object DoValidate)
        {
            Beginning:
            try { SteamDownloader.Update((bool)DoValidate); }
            catch (Exception Exception)
            {
                SteamDownloader.ReleaseLock();
                if (Exception is AggregateException)
                    Exception = Exception.InnerException;
                if (Exception is ValidatorException)
                {
                    if (Exception.Message == "Download failed" && Settings.AutoRetry)
                        goto Beginning;
                    else
                        Dispatcher.Invoke(() =>
                        {
                            SetStatus($"Error: {Exception.Message}", DarkRed);
                            FinishHandler();
                        });
                }
                else
                {
                    File.WriteAllText($@"{AppDataFolder}\LastCrash.txt", $"{Exception.Message}\n{Exception.StackTrace}");
                    Dispatcher.Invoke(() =>
                    {
                        new CrashWindow(Exception).ShowDialog();
                        Current.Shutdown();
                    });
                }
                return;
            }
        }
        internal void Update(bool DoValidate)
        {
            if (Game.IsRunning)
                SetStatus("Can't update while game is running!", DarkRed);
            else if (!IsConnectionAvailable())
                SetStatus("Can't update because internet connection is unavailable", DarkRed);
            else
            {
                SwitchButtons(false, true);
                new Thread(UpdateJob).Start(DoValidate);
            }
        }
        internal void Validate(object Sender, RoutedEventArgs Args) => Update(true);
    }
}