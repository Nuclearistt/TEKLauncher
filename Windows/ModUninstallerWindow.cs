using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TEKLauncher.ARK;
using TEKLauncher.SteamInterop;
using static System.Diagnostics.Process;
using static System.Threading.Tasks.Task;
using static System.Windows.Media.Brushes;
using static TEKLauncher.App;
using static TEKLauncher.ARK.ModManager;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.SteamInterop.SteamworksAPI;
using static TEKLauncher.UI.Message;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.Windows
{
    public partial class ModUninstallerWindow : Window
    {
        internal ModUninstallerWindow() => InitializeComponent();
        private void Close(object Sender, RoutedEventArgs Args) => Close();
        private void Drag(object Sender, MouseButtonEventArgs Args) => DragMove();
        private void Minimize(object Sender, RoutedEventArgs Args) => WindowState = WindowState.Minimized;
        private void SetStatus(string Text, SolidColorBrush Color)
        {
            Status.Foreground = Color;
            Status.Text = Text;
        }
        private async void UninstallMods(object Sender, RoutedEventArgs Args)
        {
            if (!(Instance.MWindow.ModInstallerPage is null))
                SetStatus(LocString(LocCode.MUCantUninst), DarkRed);
            else if (Game.IsRunning)
                SetStatus(LocString(LocCode.MUGameRunning), DarkRed);
            else if (ShowOptions("Warning", LocString(LocCode.MUPrompt)))
            {
                UninstallButton.IsEnabled = false;
                SetStatus(LocString(LocCode.MUUninstalling), YellowBrush);
                if ((bool)UnsubscribeMods.IsChecked)
                {
                    if (await TryDeployAsync())
                    {
                        foreach (ulong ID in SteamAPI.GetSubscribedMods())
                            if (!await SteamAPI.UnsubscribeModAsync(ID))
                            {
                                if (ShowOptions("Info", LocString(LocCode.MUFail)))
                                    break;
                                else
                                    return;
                            }
                    }
                    else if (!ShowOptions("Info", LocString(LocCode.MUFail)))
                            return;
                }
                await InitializeModsListAsync();
                foreach (Mod Mod in Mods)
                    Mod.Uninstall((bool)WorkshopMods.IsChecked, (bool)GameMods.IsChecked);
                Mods.Clear();
                if (Steam.IsSpacewarInstalled)
                {
                    bool CleanDownloadCache = (bool)DownloadCache.IsChecked, CleanWorkshopCache = (bool)WorkshopCache.IsChecked;
                    string Workshop = Steam.WorkshopPath;
                    Workshop = Workshop.Substring(0, Workshop.Length - 12);
                    if (CleanDownloadCache || CleanWorkshopCache)
                    {
                        if (!Steam.IsRunning)
                        {
                            if (CleanWorkshopCache)
                                DeletePath($@"{Workshop}\appworkshop_480.acf");
                            if (CleanDownloadCache)
                            {
                                DeletePath($@"{Workshop}\downloads");
                                DeletePath($@"{Workshop}\temp");
                            }
                        }
                        else if (ShowOptions("Info", LocString(LocCode.SteamShutdownPrompt)))
                        {
                            SetStatus(LocString(LocCode.WaitingForSteamShutdown), YellowBrush);
                            Retract();
                            Start($@"{Steam.Path}\Steam.exe", "-shutdown").WaitForExit();
                            while (Steam.IsRunning)
                                await Delay(1000);
                            if (CleanWorkshopCache)
                                DeletePath($@"{Workshop}\appworkshop_480.acf");
                            if (CleanDownloadCache)
                            {
                                DeletePath($@"{Workshop}\downloads");
                                DeletePath($@"{Workshop}\temp");
                            }
                            Start($@"{Steam.Path}\Steam.exe");
                        }
                    }
                }
                SetStatus(LocString(LocCode.MUSuccess), DarkGreen);
                UninstallButton.IsEnabled = true;
            }
        }
    }
}