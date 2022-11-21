using System.Windows.Controls;
using TEKLauncher.Windows;

namespace TEKLauncher.Tabs;

/// <summary>Tab that provides launcher-related settings.</summary>
partial class LauncherSettingsTab : ContentControl
{
    /// <summary>Initializes a new instance of Launcher settings tab.</summary>
    public LauncherSettingsTab()
    {
        InitializeComponent();
        GamePathSelector.SetPath(Game.Path!);
        Slider.Value = Steam.Client.NumberOfDownloadThreads;
    }
    /// <summary>Cleans download cache folder for current game installation.</summary>
    async void CleanDownloadCache(object sender, RoutedEventArgs e)
    {
        if (!Messages.ShowOptions("Warning", LocManager.GetString(LocCode.CleanDownloadCachePrompt)))
            return;
        await Task.Run(delegate
        {
            foreach (string file in Directory.EnumerateFiles(Steam.Client.DownloadsFolder))
                File.Delete(file);
            foreach (string directory in Directory.EnumerateDirectories(Steam.Client.DownloadsFolder))
                Directory.Delete(directory, true);
        });
        Dispatcher.Invoke(() => Notifications.Add(LocManager.GetString(LocCode.CleanDownloadCacheSuccess), "NSuccess"));
    }
    /// <summary>Deletes launcher settings file and shuts down the launcher.</summary>
    void DeleteLauncherSettings(object sender, RoutedEventArgs e)
    {
        if (!Messages.ShowOptions("Warning", LocManager.GetString(LocCode.DeleteLauncherSettingsPrompt)))
            return;
        Settings.Delete = true;
        Application.Current.Shutdown();
    }
    /// <summary>Sets new game path and shuts down the launcher.</summary>
    void PathChangedHandler(string newPath)
    {
        if (Messages.ShowOptions("Warning", LocManager.GetString(File.Exists($@"{newPath}\ShooterGame\Binaries\Win64\ShooterGame.exe") ? LocCode.GamePathChangePrompt : LocCode.GamePathChangeFilesMissing)))
        {
            Game.Path = newPath;
            Application.Current.Shutdown();
        }
        else
            GamePathSelector.SetPath(Game.Path!);
    }
    /// <summary>Updates value of the setting that the sender checkbox is assigned to.</summary>
    void UpdateSetting(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;
        var checkBox = (CheckBox)sender;
        bool newValue = checkBox.IsChecked!.Value;
        switch (((string)checkBox.Tag)[0])
        {
            case '1':
                Settings.CloseOnGameLaunch = newValue;
                if (newValue && (Steam.App.CurrentUserStatus.GameStatus != Game.Status.OwnedAndInstalled || Game.UseSpacewar) && !Messages.ShowOptions("Warning", LocManager.GetString(LocCode.LauncherCloseWarning)))
                {
                    Settings.CloseOnGameLaunch = false;
                    Task.Run(() => Dispatcher.Invoke(() => checkBox.IsChecked = false));
                }
                break;
            case '2':
                Settings.CommunismMode = newValue;
                var mainWindow = (MainWindow)Application.Current.MainWindow;
                var modsButton = mainWindow.Mods;
                var icon = (ContentControl)mainWindow.Mods.Template.FindName("Icon", mainWindow.Mods);
                if (newValue)
                {
                    mainWindow.Title = "Краснознамённый имени В.И. Ленина ТЕК Лаунчер";
                    icon.Template = (ControlTemplate)FindResource("ModsTabCM");
                    Task.Run(delegate
                    {
                        try
                        {
                            Directory.CreateDirectory($@"{App.AppDataFolder}\CM");
                            string imagePath = $@"{App.AppDataFolder}\CM\Image.jpg";
                            string imageDownloadPath = $@"{App.AppDataFolder}\CM\Dw_Image.jpg";
                            if (!File.Exists(imagePath) && Downloader.DownloadFileAsync(imageDownloadPath, new(), "https://drive.google.com/uc?export=download&id=19LvV2jtDDjtg9bSd8-Ec8mqUGyOaVV8a").Result)
                                File.Move(imageDownloadPath, imagePath);
                            string audioPath = $@"{App.AppDataFolder}\CM\Audio.wav";
                            string audioDownloadPath = $@"{App.AppDataFolder}\CM\Dw_Audio.wav";
                            if (!File.Exists(audioPath) && Downloader.DownloadFileAsync(audioDownloadPath, new(), "https://drive.google.com/uc?export=download&id=1f6qBEQKhFDELItES_CvrSEv0pep3b5r-").Result)
                                File.Move(audioDownloadPath, audioPath);
                            App.Player = new(audioPath);
                            App.Player.PlayLooping();
                        }
                        catch { }
                    });
                }
                else
                {
                    mainWindow.Title = "TEK Launcher";
                    icon.Template = (ControlTemplate)FindResource("ModsTab");
                    App.Player?.Stop();
                }
                break;
        }
    }
    /// <summary>Sets new game path and shuts down the launcher.</summary>
    void ValueChangedHandler(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (IsLoaded)
            Steam.Client.NumberOfDownloadThreads = (int)e.NewValue;
    }
}