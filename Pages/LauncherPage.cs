using System.IO;
using System.Windows;
using System.Windows.Controls;
using TEKLauncher.ARK;
using TEKLauncher.Data;
using static System.IO.Directory;
using static System.Windows.Application;
using static TEKLauncher.App;
using static TEKLauncher.CommunismMode;
using static TEKLauncher.Data.Settings;
using static TEKLauncher.UI.Message;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.Pages
{
    public partial class LauncherPage : Page
    {
        public LauncherPage()
        {
            InitializeComponent();
            GamePath.SetPath(Game.Path);
            CloseOnGameRun.IsChecked = Settings.CloseOnGameRun;
            Communism.IsChecked = Settings.CommunismMode;
        }
        private void ChangeGamePath(object Sender, RoutedEventArgs Args)
        {
            if (FileExists($@"{GamePath.Text}\ShooterGame\Binaries\Win64\ShooterGame.exe"))
            {
                if (ShowOptions("Warning", "Are you sure you want to change game path used? It'll close the launcher to apply changes"))
                {
                    ARKPath = GamePath.Text;
                    Current.Shutdown();
                }
                else
                    GamePath.SetPath(Game.Path);
            }
            else
            {
                Show("Warning", "Can't use that folder as it doesn't have game files");
                GamePath.SetPath(Game.Path);
            }
        }
        private void CleanDownloadCache(object Sender, RoutedEventArgs Args)
        {
            if (ShowOptions("Warning", "Are you sure you want to delete all validations and incomplete downloads progress?"))
            {
                DeleteDirectory(DownloadsDirectory);
                CreateDirectory(DownloadsDirectory).Attributes = FileAttributes.Directory | FileAttributes.Hidden;
            }
        }
        private void DeleteLauncherSettings(object Sender, RoutedEventArgs Args)
        {
            if (ShowOptions("Warning", "Are you sure you want to delete all launcher settings? It'll wipe stored game path, selected language and launch parameters, and then close the launcher to apply changes"))
            {
                DeleteSettings = true;
                Current.Shutdown();
            }
        }
        private void SetCloseOnGameRun(object Sender, RoutedEventArgs Args)
        {
            if (IsLoaded)
                Settings.CloseOnGameRun = (bool)CloseOnGameRun.IsChecked;
        }
        private void SetCommunism(object Sender, RoutedEventArgs Args)
        {
            if (IsLoaded)
                Set(Settings.CommunismMode = (bool)Communism.IsChecked);
        }
    }
}