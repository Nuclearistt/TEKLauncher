using System.IO;
using System.Windows;
using System.Windows.Input;
using static System.Windows.Application;
using static System.Windows.Media.Brushes;
using static Microsoft.Win32.Registry;
using static TEKLauncher.App;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.Data.Settings;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.Windows
{
    public partial class StartupWindow : Window
    {
        internal StartupWindow()
        {
            InitializeComponent();
            if (LocCulture == "ar")
            {
                FreeDiskSpaceStack.FlowDirection = FlowDirection.RightToLeft;
                FreeDiskSpaceStack.HorizontalAlignment = HorizontalAlignment.Left;
            }
            string SteamGamePath = (string)LocalMachine?.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 346110")?.GetValue("InstallLocation");
            if (!(SteamGamePath is null) && FileExists($@"{SteamGamePath}\ShooterGame\Binaries\Win64\ShooterGame.exe"))
            {
                Path = SteamGamePath;
                Selector.Tag = "D";
                Selector.SetPath(SteamGamePath);
                ContinueButton.IsEnabled = true;
            }
        }
        private string Path;
        private void Close(object Sender, RoutedEventArgs Args) => Current.Shutdown();
        private void Continue(object Sender, RoutedEventArgs Args)
        {
            if (InstallMode)
                Directory.CreateDirectory(Path);
            ARKPath = Path;
            Close();
            Instance.Initialize();
        }
        private void Drag(object Sender, MouseButtonEventArgs Args) => DragMove();
        private void Minimize(object Sender, RoutedEventArgs Args) => WindowState = WindowState.Minimized;
        private void SelectFolder(object Sender, RoutedEventArgs Args)
        {
            ContinueButton.Content = (InstallMode = !FileExists($@"{Path = Selector.Text}\ShooterGame\Binaries\Win64\ShooterGame.exe")) ? LocString(LocCode.Install) : LocString(LocCode.Continue);
            FreeDiskSpaceStack.Visibility = RequiredSpace.Visibility = InstallMode ? Visibility.Visible : Visibility.Hidden;
            if (InstallMode)
            {
                long FreeSpace = GetFreeSpace(Path);
                Path += @"\ArkGameData";
                FreeDiskSpaceUnit.Foreground = FreeDiskSpace.Foreground = (ContinueButton.IsEnabled = FreeSpace > 118111600639L) ? DarkGreen : DarkRed;
                FreeDiskSpace.Text = ConvertBytesSep(FreeSpace, out string Unit);
                FreeDiskSpaceUnit.Text = Unit;
            }
            else
                ContinueButton.IsEnabled = true;
        }
    }
}