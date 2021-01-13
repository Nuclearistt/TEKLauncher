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
using TEKLauncher.SteamInterop;
using TEKLauncher.SteamInterop.Network;
using TEKLauncher.Windows;
using static System.Enum;
using static System.Environment;
using static System.Diagnostics.Process;
using static System.IO.Directory;
using static System.Windows.Application;
using static System.Windows.Media.Brushes;
using static TEKLauncher.App;
using static TEKLauncher.Data.Links;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.UI.Message;
using static TEKLauncher.UI.Notifications;
using static TEKLauncher.Utils.TEKArchive;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.Pages
{
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
            if (LocCulture == "es")
                foreach (Panel Stack in LPGrid.Children)
                    foreach (CheckBox Checkbox in Stack.Children)
                        Checkbox.FontSize = 18D;
            else if (LocCulture == "el")
                foreach (Button Button in OptionsGrid.Children)
                    Button.FontSize = 16D;
            else if (LocCulture == "ar")
            {
                StatusStack.FlowDirection = FlowDirection.RightToLeft;
                foreach (Panel Stack in ValidationBlock.Children)
                    Stack.FlowDirection = FlowDirection.RightToLeft;
            }
            UseGlobalFonts.IsChecked = Settings.UseGlobalFonts;
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
        private void DownloadBeganHandler() => SetStatus(string.Format(LocString(LocCode.SPDownloading), InstallingItem), YellowBrush);
        private void FailedToDownload()
        {
            SetStatus(string.Format(LocString(LocCode.SPDownloadFailed), InstallingItem), DarkRed);
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
                AddImage(LocString(LocCode.CantFixBEGameRunning), "Error");
            else if (!FileExists(Game.BEExecutablePath))
                AddImage(LocString(LocCode.CantFixBEExeMissing), "Error");
            else
            {
                InstallingItem = LocString(LocCode.BattlEye);
                PreparingToDownload();
                string ArchivePath = $@"{AppDataFolder}\BattlEye.ta";
                ProgressBar.SetDownloadMode();
                if (await Downloader.TryDownloadFileAsync(ArchivePath, $"{FilesStorage}BattlEye.ta", GDriveBattlEyeFile))
                {
                    Start(Game.BEExecutablePath, "4 0").WaitForExit();
                    DeletePath($@"{Game.Path}\ShooterGame\Binaries\Win64\BattlEye");
                    DeletePath($@"{GetFolderPath(SpecialFolder.ProgramFilesX86)}\Common Files\BattlEye\BEService_ark.exe");
                    DeletePath($@"{GetFolderPath(SpecialFolder.LocalApplicationData)}\BattlEye\ark");
                    SetStatus(LocString(LocCode.ExtractingArchive), YellowBrush);
                    if (await DecompressArchiveAsync(ArchivePath, $@"{Game.Path}\ShooterGame\Binaries\Win64\BattlEye"))
                    {
                        File.Delete(ArchivePath);
                        Start(Game.BEExecutablePath, "1 0").WaitForExit();
                        SetStatus(LocString(LocCode.BEFixSuccess), DarkGreen);
                    }
                    else
                        SetStatus(LocString(LocCode.FailedToExtract), DarkRed);
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
                AddImage(LocString(LocCode.CantFixBloom), "Error");
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
            AddImage(LocString(LocCode.BloomFixSuccess), "Success");
        }
        private async void InstallRequirements(object Sender, RoutedEventArgs Args)
        {
            InstallingItem = LocString(LocCode.Requirements);
            PreparingToDownload();
            string ArchivePath = $@"{AppDataFolder}\CommonRedist.ta";
            ProgressBar.SetDownloadMode();
            if (await Downloader.TryDownloadFileAsync(ArchivePath, $"{FilesStorage}CommonRedist.ta", GDriveCommonRedistFile))
            {
                string CommonRedistFolder = $@"{AppDataFolder}\CommonRedist";
                SetStatus(LocString(LocCode.ExtractingArchive), YellowBrush);
                if (await DecompressArchiveAsync(ArchivePath, CommonRedistFolder))
                {
                    File.Delete(ArchivePath);
                    SetStatus(LocString(LocCode.InstallingReq), YellowBrush);
                    Start($@"{CommonRedistFolder}\DirectX\DXSETUP.exe", "/silent").WaitForExit();
                    Start($@"{CommonRedistFolder}\MSVCP\2010\vcredist_x64.exe", "/q /norestart").WaitForExit();
                    Start($@"{CommonRedistFolder}\MSVCP\2010\vcredist_x86.exe", "/q /norestart").WaitForExit();
                    Start($@"{CommonRedistFolder}\MSVCP\2012\vcredist_x64.exe", "/install /quiet /norestart").WaitForExit();
                    Start($@"{CommonRedistFolder}\MSVCP\2012\vcredist_x86.exe", "/install /quiet /norestart").WaitForExit();
                    Start($@"{CommonRedistFolder}\MSVCP\2013\vcredist_x64.exe", "/install /quiet /norestart").WaitForExit();
                    Start($@"{CommonRedistFolder}\MSVCP\2013\vcredist_x86.exe", "/install /quiet /norestart").WaitForExit();
                    DeleteDirectory(CommonRedistFolder);
                    SetStatus(LocString(LocCode.InstallReqSuccess), DarkGreen);
                }
                else
                    SetStatus(LocString(LocCode.FailedToExtract), DarkRed);
                SwitchButtons(true);
            }
            else
                FailedToDownload();
            Instance.MWindow.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
        }
        private void InstallSpacewar(object Sender, RoutedEventArgs Args)
        {
            if (Steam.IsSpacewarInstalled)
                AddImage(LocString(LocCode.SpacewarInstalled), "Success");
            else if (!Steam.IsRunning)
                AddImage(LocString(LocCode.SpacewarNeedsSteam), "Error");
            else
                Execute("steam://install/480");
        }
        private void InvertUseGlobalFontsCB() => UseGlobalFonts.IsChecked = !(bool)UseGlobalFonts.IsChecked;
        private void InvokeInvertUseGlobalFontsCB() => Dispatcher.Invoke(InvertUseGlobalFontsCB);
        private void LoadedHandler(object Sender, RoutedEventArgs Args)
        {
            UpdateCreamAPIButtons();
            if (InstallMode)
            {
                Update(false);
                Show("Info", LocString(LocCode.GameDownloadBegan));
            }
        }
        private void PreparingToDownload()
        {
            SwitchButtons(false);
            SetStatus(string.Format(LocString(LocCode.SPPrepToDownload), InstallingItem), YellowBrush);
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
        private void ReapplyCreamAPI(object Sender, RoutedEventArgs Args)
        {
            if (Game.IsRunning)
                AddImage(LocString(LocCode.CantReapplyGameRunning), "Error");
            else
            {
                CreamAPI.Install();
                AddImage(LocString(LocCode.ReapplySuccess), "Success");
            }
        }
        private void SetStatus(string Text, SolidColorBrush Color)
        {
            Status.Foreground = Color;
            Status.Text = Text;
        }
        private async void SetUseGlobalFonts(object Sender, RoutedEventArgs Args)
        {
            if (!IsLoaded)
                return;
            if (Settings.UseGlobalFonts)
                Settings.UseGlobalFonts = false;
            else
            {
                string GlobalFolder = $@"{Game.Path}\ShooterGame\Content\Localization\Game\global", LocFolder = $@"{Game.Path}\ShooterGame\Content\Localization\Game\{LaunchParameters.GameCultureCodes[Settings.GameLang]}";
                if (Game.GlobalFontsInstalled)
                {
                    if (Exists(LocFolder))
                    {
                        File.Copy($@"{LocFolder}\ShooterGame.archive", $@"{GlobalFolder}\ShooterGame.archive", true);
                        File.Copy($@"{LocFolder}\ShooterGame.locres", $@"{GlobalFolder}\ShooterGame.locres", true);
                    }
                    Settings.UseGlobalFonts = true;
                }
                else
                {
                    InstallingItem = LocString(LocCode.GlobalFonts);
                    PreparingToDownload();
                    string ArchivePath = $@"{AppDataFolder}\GlobalFonts.ta";
                    ProgressBar.SetDownloadMode();
                    if (await Downloader.TryDownloadFileAsync(ArchivePath, $"{FilesStorage}GlobalFonts.ta", GDriveGlobalFontsFile))
                    {
                        SetStatus(LocString(LocCode.ExtractingArchive), YellowBrush);
                        if (await DecompressArchiveAsync(ArchivePath, GlobalFolder))
                        {
                            File.Delete(ArchivePath);
                            if (Exists(LocFolder))
                            {
                                File.Copy($@"{LocFolder}\ShooterGame.archive", $@"{GlobalFolder}\ShooterGame.archive", true);
                                File.Copy($@"{LocFolder}\ShooterGame.locres", $@"{GlobalFolder}\ShooterGame.locres", true);
                            }
                            SetStatus(LocString(LocCode.GlobalFontsSuccess), DarkGreen);
                            Settings.UseGlobalFonts = true;
                        }
                        else
                        {
                            SetStatus(LocString(LocCode.FailedToExtract), DarkRed);
                            new Thread(InvokeInvertUseGlobalFontsCB).Start();
                        }
                        SwitchButtons(true);
                    }
                    else
                    {
                        FailedToDownload();
                        new Thread(InvokeInvertUseGlobalFontsCB).Start();
                    }
                    Instance.MWindow.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                }
            }
        }
        private void SwitchButtons(bool State, bool Update = false)
        {
            UseGlobalFonts.IsEnabled = InstallReqButton.IsEnabled = ValidateButton.IsEnabled = UnlockButton.IsEnabled = FixBEButton.IsEnabled = State;
            if (Update)
                UpdateButton.Content = LocString((LocCode)Parse(typeof(LocCode), ((VectorImage)UpdateButton.Template.FindName("Icon", UpdateButton)).Source = State ? "Update" : "Pause"));
            else
                UpdateButton.IsEnabled = State;
        }
        private void SwitchCreamAPI(object Sender, RoutedEventArgs Args)
        {
            bool CreamAPIInstalled = CreamAPI.IsInstalled;
            if (Game.IsRunning)
                AddImage(LocString(CreamAPIInstalled ? LocCode.CantUninstallCA : LocCode.CantInstallCA), "Error");
            else
            {
                if (CreamAPIInstalled)
                    CreamAPI.Uninstall();
                else
                    CreamAPI.Install();
                UpdateCreamAPIButtons();
                AddImage(LocString(CreamAPIInstalled ? LocCode.UninstallCASuccess : LocCode.InstallCASuccess), "Success");
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
                SetStatus(LocString(LocCode.CantUnlockSkins), DarkRed);
            else
            {
                string LocalProfiles = $@"{Game.Path}\ShooterGame\Saved\LocalProfiles";
                CreateDirectory(LocalProfiles);
                InstallingItem = LocString(LocCode.LocalProfile);
                PreparingToDownload();
                ProgressBar.SetDownloadMode();
                if (await Downloader.TryDownloadFileAsync($@"{LocalProfiles}\PlayerLocalData.arkprofile", $"{FilesStorage}PlayerLocalData.arkprofile", GDriveLocalProfileFile))
                {
                    SetStatus(LocString(LocCode.SkinUnlockSuccess), DarkGreen);
                    SwitchButtons(true);
                }
                else
                    FailedToDownload();
                Instance.MWindow.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
            }
        }
        private void Update(object Sender, RoutedEventArgs Args)
        {
            if ((string)UpdateButton.Content == LocString(LocCode.Update))
                Update(false);
            else
                SteamDownloader.Pause();
        }
        private void UpdateCreamAPIButtons()
        {
            bool CreamAPIInstalled = CreamAPI.IsInstalled;
            SwitchCreamAPIButton.Content = CreamAPIInstalled ? LocString(LocCode.Uninstall) : LocString(LocCode.Install);
            ((VectorImage)SwitchCreamAPIButton.Template.FindName("Icon", SwitchCreamAPIButton)).Source = CreamAPIInstalled ? "Delete" : "Install";
            ReapplyCreamAPIButton.IsEnabled = CreamAPIInstalled;
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
                    if (Exception.Message == LocString(LocCode.DownloadFailed) && Settings.AutoRetry)
                        goto Beginning;
                    else
                        Dispatcher.Invoke(() =>
                        {
                            SetStatus(string.Format(LocString(LocCode.ValidatorExc), Exception.Message), DarkRed);
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
                SetStatus(LocString(LocCode.CantUpdateGameRunning), DarkRed);
            else if (!IsConnectionAvailable())
                SetStatus(LocString(LocCode.CantUpdateNoInternet), DarkRed);
            else
            {
                SwitchButtons(false, true);
                new Thread(UpdateJob).Start(DoValidate);
            }
        }
        internal void Validate(object Sender, RoutedEventArgs Args) => Update(true);
    }
}