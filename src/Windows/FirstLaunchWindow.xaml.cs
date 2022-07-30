using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Win32;
using TEKLauncher.Controls;

namespace TEKLauncher.Windows;

/// <summary>Window displayed when user launches the application for the first time.</summary>
partial class FirstLaunchWindow : TEKWindow
{
    /// <summary>Currently selected game path.</summary>
    string? _path;
    /// <summary>Begin installation button of Game installation tab.</summary>
    Button _beginInstallationButton = null!;
    /// <summary>Continue button of Path selection tab.</summary>
    Button _continueButton = null!;
    /// <summary>Steam icon of Path selection tab.</summary>
    Image _steamIcon = null!;
    /// <summary>Disk free space text run of Game installation tab.</summary>
    Run _freeSpace = null!;
    /// <summary>Status text block of Path selection tab.</summary>
    TextBlock _status = null!;
    /// <summary>Initializes a new First launch window.</summary>
    public FirstLaunchWindow() => InitializeComponent();
    /// <summary>Navigates the window back to Start tab.</summary>
    void Back(object sender, RoutedEventArgs e) => Root.Template = (ControlTemplate)FindResource("StartTab");
    /// <summary>Locks selected path and proceeds to initializing the app.</summary>
    void Continue(object sender, RoutedEventArgs e)
    {
        if (_path!.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)) && !Messages.ShowOptions("Warning", LocManager.GetString(LocCode.GamePathInUserFolder)))
            return;
        Game.Path = _path;
        Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        Close();
        App.Initialize();
        Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
    }
    /// <summary>Initializes Game installation tab.</summary>
    void GameInstallationTabLoadedHandler(object sender, RoutedEventArgs e)
    {
        _path = null;
        var template = Root.Template;
        _beginInstallationButton = (Button)template.FindName("BeginInstallationButton", Root);
        _freeSpace = (Run)template.FindName("FreeSpace", Root);
    }
    /// <summary>Processes new path selection in Game installation tab.</summary>
    /// <param name="newPath">New path that has been selected.</param>
    void GIPathChangedHandler(string newPath)
    {
        long freeSpace = WinAPI.GetDiskFreeSpace(newPath);
        bool enoughSpace = freeSpace >= 139586437120; //130 GB
        _freeSpace.Foreground = new SolidColorBrush(enoughSpace ? Color.FromRgb(0x0A, 0xA6, 0x3E) : Color.FromRgb(0x9E, 0x23, 0x13));
        _freeSpace.Text = LocManager.BytesToString(freeSpace);
        _beginInstallationButton.IsEnabled = enoughSpace;
        if (enoughSpace)
            _path = newPath;
    }
    /// <summary>Navigates to Path selection tab.</summary>
    void Option1(object sender, RoutedEventArgs e) => Root.Template = (ControlTemplate)FindResource("PathSelectionTab");
    /// <summary>Navigates to Game installation tab.</summary>
    void Option2(object sender, RoutedEventArgs e) => Root.Template = (ControlTemplate)FindResource("GameInstallationTab");
    /// <summary>Initializes Path selection tab.</summary>
    void PathSelectionTabLoadedHandler(object sender, RoutedEventArgs e)
    {
        _path = null;
        var template = Root.Template;
        var selector = (PathSelector)template.FindName("Selector", Root);
        _continueButton = (Button)template.FindName("ContinueButton", Root);
        _steamIcon = (Image)template.FindName("SteamIcon", Root);
        _status = (TextBlock)template.FindName("Status", Root);
        string? steamGamePath = Steam.App.GamePath;
        if (steamGamePath is not null)
        {
            selector.SetPath(steamGamePath);
            PSPathChangedHandler(steamGamePath);
            _steamIcon.Visibility = Visibility.Visible;
        }
    }
    /// <summary>Processes new path selection in Path selection tab.</summary>
    /// <param name="newPath">New path that has been selected.</param>
    void PSPathChangedHandler(string newPath)
    {
        if (_steamIcon.Visibility == Visibility.Visible)
            _steamIcon.Visibility = Visibility.Collapsed;
        bool filesExist = File.Exists($@"{newPath}\ShooterGame\Binaries\Win64\ShooterGame.exe");
        _continueButton.IsEnabled = filesExist;
        if (filesExist)
        {
            _path = newPath;
            _status.Foreground = new SolidColorBrush(Color.FromRgb(0x0A, 0xA6, 0x3E));
            _status.Text = LocManager.GetString(LocCode.GameFilesFound);
        }
        else
        {
            _status.Foreground = new SolidColorBrush(Color.FromRgb(0x9E, 0x23, 0x13));
            _status.Text = LocManager.GetString(LocCode.GameFilesNotFound);
        }
    }
}