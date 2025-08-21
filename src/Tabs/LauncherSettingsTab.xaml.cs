using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Media;
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
    }
    /// <summary>Cleans download cache folder for current game installation.</summary>
    async void CleanDownloadCache(object sender, RoutedEventArgs e)
    {
        if (!Messages.ShowOptions("Warning", LocManager.GetString(LocCode.CleanDownloadCachePrompt)))
            return;
        await Task.Run(delegate
        {
            TEKSteamClient.AppMng!.LockItemDescs();
            unsafe
            {
				var desc = TEKSteamClient.AppMng.GetItemDesc(null);
                while (desc != null)
                {
                    if (desc->Status.HasFlag(TEKSteamClient.AmItemStatus.Job))
                        TEKSteamClient.AppMng.CancelJob(ref Unsafe.AsRef<TEKSteamClient.AmItemDesc>(desc));
                    desc = desc->Next;
                }
			}
			TEKSteamClient.AppMng.UnlockItemDescs();
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
    unsafe void UpdateSetting(object sender, RoutedEventArgs e)
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
            case '3':
                Settings.PreAquatica = newValue;
                if (TEKSteamClient.Ctx == null)
                    break;
				static TEKSteamClient.AmItemDesc* getDesc(uint depotId)
				{
					var itemId = new TEKSteamClient.ItemId { AppId = 346110, DepotId = depotId, WorkshopItemId = 0 };
					return TEKSteamClient.AppMng!.GetItemDesc((TEKSteamClient.ItemId*)Unsafe.AsPointer(ref itemId));
				}
				var desc = getDesc(346111);
				if (desc != null && desc->CurrentManifestId != 0)
                {
					bool updAvailable = Settings.PreAquatica ? desc->CurrentManifestId != 8075379529797638112 : desc->Status.HasFlag(TEKSteamClient.AmItemStatus.UpdAvailable);
					var gameVersion = ((MainWindow)Application.Current.MainWindow).GameVersion;
					gameVersion.Text = LocManager.GetString(updAvailable ? LocCode.Outdated : LocCode.Latest);
					gameVersion.Foreground = updAvailable ? Brushes.Yellow : new SolidColorBrush(Color.FromRgb(0x0A, 0xA6, 0x3E));
				}
				foreach (var dlc in ARK.DLC.List)
				{
					if (dlc.CurrentStatus is not (ARK.DLC.Status.Installed or ARK.DLC.Status.UpdateAvailable))
						continue;
					desc = getDesc(dlc.DepotId);
					if (desc == null)
						continue;
					dlc.CurrentStatus = (Settings.PreAquatica ? desc->CurrentManifestId != dlc.DepotId switch
					{
						346114 => 5573587184752106093,
						375351 => 8265777340034981821,
						375354 => 7952753366101555648,
						375357 => 1447242805278740772,
						473851 => 2551727096735353757,
						473854 => 847717640995143866,
						473857 => 1054814513659387220,
						1318685 => 8189621638927588129,
						1691801 => 3147973472387347535,
						1887561 => 580528532335699271,
						_ => 0
					} : desc->Status.HasFlag(TEKSteamClient.AmItemStatus.UpdAvailable)) ? ARK.DLC.Status.UpdateAvailable : ARK.DLC.Status.Installed;
				}
				break;
        }
    }
}