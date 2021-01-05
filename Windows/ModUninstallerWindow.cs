using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TEKLauncher.ARK;
using TEKLauncher.SteamInterop;
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
                    string Workshop = Steam.WorkshopPath;
                    Workshop = Workshop.Substring(0, Workshop.Length - 12);
                    if ((bool)WorkshopCache.IsChecked)
                        DeletePath($@"{Workshop}\appworkshop_480.acf");
                    if ((bool)DownloadCache.IsChecked)
                    {
                        DeletePath($@"{Workshop}\downloads");
                        DeletePath($@"{Workshop}\temp");
                    }
                }
                SetStatus(LocString(LocCode.MUSuccess), DarkGreen);
                UninstallButton.IsEnabled = true;
            }
        }
    }
}