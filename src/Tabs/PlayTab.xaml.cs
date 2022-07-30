using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace TEKLauncher.Tabs;

/// <summary>Main tab that provides general game options and the button for launching the game.</summary>
partial class PlayTab : ContentControl
{
    /// <summary>Initializes a new instance of Play tab.</summary>
    public PlayTab()
    {
        InitializeComponent();
        string cmImagePath = $@"{App.AppDataFolder}\CM\Image.jpg";
        Image.Source = new BitmapImage(new(Settings.CommunismMode && File.Exists(cmImagePath) ? cmImagePath : "pack://application:,,,/res/img/PlayTab.jpg"));
    }
    /// <summary>Launches the game.</summary>
    void Launch(object sender, RoutedEventArgs e) => Game.Launch(null);
    /// <summary>Sets new game or launcher language.</summary>
    void SelectionChangedHandler(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;
        if (sender == LauncherLanguages)
        {
            LocManager.SetCurrentLanguage(LauncherLanguages.SelectedIndex);
            Messages.Show("Info", LocManager.GetString(LocCode.LanguageChangeInfo));
        }
        else
            Game.Language = GameLanguages.SelectedIndex;
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
            case '0': Game.RunAsAdmin = newValue; break;
            case '1': Game.UseBattlEye = newValue; break;
        }
    }
}