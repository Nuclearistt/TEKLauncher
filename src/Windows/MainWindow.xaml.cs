﻿using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using TEKLauncher.Tabs;

namespace TEKLauncher.Windows;

/// <summary>Main window of the launcher.</summary>
partial class MainWindow : TEKWindow
{
	/// <summary>Index of the current tab.</summary>
	int _currentTabIndex;
	/// <summary>Height of a menu button.</summary>
	double _menuButtonHeight;
	/// <summary>Singleton instance of Game options tab.</summary>
	static GameOptionsTab? s_gameOptionsTab;
	/// <summary>Singleton instance of tab navigation animation.</summary>
	static readonly DoubleAnimation s_naviagtionAnimation = new(0, TimeSpan.FromMilliseconds(250)) { EasingFunction = new QuadraticEase() };
	/// <summary>Initializes a new main window.</summary>
	public MainWindow()
	{
		InitializeComponent();
		if (Settings.CommunismMode)
		{
			Title = "Краснознамённый имени В.И. Ленина ТЕК Лаунчер";
			string audioPath = $@"{App.AppDataFolder}\CM\Audio.wav";
			if (File.Exists(audioPath))
			{
				App.Player = new(audioPath);
				App.Player.PlayLooping();
			}
		}
		Notifications.Initialize(NotificationStack.Children);
		LauncherVersion.Text = App.Version;
		TabFrame.Child = new PlayTab();
		CheckForUpdates();
	}
	/// <summary>Checks for launcher, game and DLC updates.</summary>
	async void CheckForUpdates()
	{
		//Set initial displayed game version
		Dispatcher.Invoke(delegate
		{
			if (Game.IsCorrupted)
			{
				GameVersion.Text = LocManager.GetString(LocCode.None);
				GameVersion.Foreground = new SolidColorBrush(Color.FromRgb(0x9E, 0x23, 0x13));
			}
			else
				GameVersion.Text = LocManager.GetString(LocCode.NA);
		});
		//Check for launcher updates
		string? versionString = await Downloader.DownloadStringAsync("https://teknology-hub.com/software/tek-launcher/version", "https://de.teknology-hub.com/software/tek-launcher/version");
		if (versionString is null)
		{
			var release = await Downloader.DownloadJsonAsync<Release>("https://api.github.com/repos/Nuclearistt/TEKLauncher/releases/latest");
			versionString = release.TagName?[1..];
		}
		if (versionString is not null)
		{
			var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
			if (Version.TryParse(versionString, out var onlineVersion) && onlineVersion > currentVersion)
				Dispatcher.Invoke(delegate
				{
					LauncherVersion.Foreground = Brushes.Yellow;
					Notifications.Add(LocManager.GetString(LocCode.LauncherUpdateAvailable), LocManager.GetString(LocCode.Update), () => new LauncherUpdateWindow().Show(), true);
				});
		}
		Dispatcher.Invoke(() => LauncherVersionBlock.Inlines.Remove(LauncherVersionBlock.Inlines.LastInline));
		//Initialize tek-steamclient
		if (!File.Exists(TEKSteamClient.DllPath))
		{
			Dispatcher.Invoke(() => Notifications.Add("Downloading tek-steamclient library...", "", () => { }));
			if (!Downloader.DownloadFileAsync(TEKSteamClient.DllPath, new(), "https://teknology-hub.com/software/tek-steamclient/releases/latest/win-x86_64-static/libtek-steamclient-1.dll", "https://de.teknology-hub.com/software/tek-steamclient/releases/latest/win-x86_64-static/libtek-steamclient-1.dll").Result)
				throw new TEKSteamClient.Exception("Failed to download libtek-steamclient-1.dll");
		}
		NativeLibrary.Load(TEKSteamClient.DllPath);
		TEKSteamClient.Ctx = new();
		TEKSteamClient.AppMng = new(TEKSteamClient.Ctx, Game.Path!);
		if (TEKSteamClient.AppMng.IsInvalid)
			throw new TEKSteamClient.Exception(TEKSteamClient.AppMng.CreationError.Message);
		var res = TEKSteamClient.AppMng.SetWorkshopDir($@"{Game.Path}\Mods");
		if (!res.Success)
			throw new TEKSteamClient.Exception(res.Message);
		res = TEKSteamClient.Ctx.SyncS3Manifest("https://api.teknology-hub.com/s3");
		if (!res.Success)
		{
			if (res.Uri != 0)
				Marshal.FreeHGlobal(res.Uri);
			res = TEKSteamClient.Ctx.SyncS3Manifest("https://de.api.teknology-hub.com/s3");
		}
		if (!res.Success)
			Dispatcher.Invoke(() => Notifications.Add("Failed to synchronize tek-s3 manifest", "NError"));
		//Check for updates
		if (await Task.Run(() => TEKSteamClient.AppMng.CheckForUpdates(20000).Success).ConfigureAwait(false))
		{
			unsafe
			{
				static TEKSteamClient.AmItemDesc* getDesc(uint depotId)
				{
					var itemId = new TEKSteamClient.ItemId { AppId = 346110, DepotId = depotId, WorkshopItemId = 0 };
					return TEKSteamClient.AppMng!.GetItemDesc((TEKSteamClient.ItemId*)Unsafe.AsPointer(ref itemId));
				}
				var desc = getDesc(346111);
				if (desc != null && desc->CurrentManifestId != 0)
				{
					Dispatcher.Invoke(delegate
					{
						bool updAvailable = desc->Status.HasFlag(TEKSteamClient.AmItemStatus.UpdAvailable);
						GameVersion.Text = LocManager.GetString(updAvailable ? LocCode.Outdated : LocCode.Latest);
						GameVersion.Foreground = updAvailable ? Brushes.Yellow : new SolidColorBrush(Color.FromRgb(0x0A, 0xA6, 0x3E));
						if (updAvailable)
							Notifications.Add(LocManager.GetString(LocCode.GameUpdateAvailable), LocManager.GetString(LocCode.Update), delegate
							{
								if (TabFrame.Child is not GameOptionsTab)
								{
									s_gameOptionsTab ??= new GameOptionsTab();
									Navigate(s_gameOptionsTab);
								}
								s_gameOptionsTab!.RunTask(false);
							});
					});
				}
				foreach (var dlc in ARK.DLC.List)
				{
					if (dlc.CurrentStatus != ARK.DLC.Status.Installed)
						continue;
					desc = getDesc(dlc.DepotId);
					if (desc != null && desc->Status.HasFlag(TEKSteamClient.AmItemStatus.UpdAvailable))
						dlc.CurrentStatus = ARK.DLC.Status.UpdateAvailable;
				}
			}
			if (Array.Exists(ARK.DLC.List, d => d.CurrentStatus == ARK.DLC.Status.UpdateAvailable))
				Dispatcher.Invoke(() => Notifications.Add(LocManager.GetString(LocCode.DLCUpdatesAvailable), LocManager.GetString(LocCode.Update), delegate
				{
					if (TabFrame.Child is not DLCTab)
						Navigate(new DLCTab());
				}));
		}
	}
	/// <summary>Checks whether the window is eligible for closing.</summary>
	void ClosingHandler(object sender, CancelEventArgs e)
	{
		if ((Steam.App.CurrentUserStatus.GameStatus != Game.Status.OwnedAndInstalled || Game.UseSpacewar) && Game.IsRunning && !Messages.ShowOptions("Warning", LocManager.GetString(LocCode.LauncherCloseWarning)))
		{
			e.Cancel = true;
			return;
		}
		bool activeTasksPresent = GameOptionsTab.IsSteamTaskActive;
		if (!activeTasksPresent)
			foreach (var window in Application.Current.Windows)
				if (window is UpdaterWindow updaterWindow && updaterWindow.IsSteamTaskActive)
				{
					activeTasksPresent = true;
					break;
				}
		if (activeTasksPresent && !Messages.ShowOptions("Warning", LocManager.GetString(LocCode.LauncherClosePrompt)))
			e.Cancel = true;
	}
	/// <summary>Initializes the menu.</summary>
	void MenuLoadedHandler(object sender, RoutedEventArgs e)
	{
		_menuButtonHeight = Play.ActualHeight;
		MenuLine.Y2 = _menuButtonHeight - 1.5;
		foreach (RadioButton button in Menu.Children)
		{
			string key = string.Concat(button.Name, "Tab");
			var icon = (ContentControl)button.Template.FindName("Icon", button);
			icon.Template = (ControlTemplate)FindResource(Settings.CommunismMode && button == Mods ? "ModsTabCM" : key);
			button.GroupName = "Menu";
			button.Content = LocManager.GetString(Enum.Parse<LocCode>(key));
			button.Checked += NavigatedHandler;
		}
	}
	/// <summary>Performs tab navigation when a menu button is pressed.</summary>
	void NavigatedHandler(object sender, RoutedEventArgs args)
	{
		int newTabIndex = Menu.Children.IndexOf((RadioButton)sender);
		if (newTabIndex == 4 && Steam.App.CurrentUserStatus.GameStatus == Game.Status.OwnedAndInstalled && !Game.UseSpacewar)
		{
			Messages.Show("Warning", LocManager.GetString(LocCode.ModsOnSteamWarning));
			Task.Run(() => Dispatcher.Invoke(() =>
			{
				((RadioButton)sender).IsChecked = false;
				var checkedButton = (RadioButton)Menu.Children[_currentTabIndex];
				checkedButton.Checked -= NavigatedHandler;
				checkedButton.IsChecked = true;
				checkedButton.Checked += NavigatedHandler;
			}));
			return;
		}
		int difference = newTabIndex - _currentTabIndex;
		if (difference != 0)
		{
			double firstValue, secondValue;
			DependencyProperty firstProperty, secondProperty;
			if (difference > 0)
			{
				firstValue = _menuButtonHeight * (newTabIndex + 1) - 1.5;
				secondValue = _menuButtonHeight * newTabIndex + 1.5;
				firstProperty = Line.Y2Property;
				secondProperty = Line.Y1Property;
			}
			else
			{
				firstValue = _menuButtonHeight * newTabIndex + 1.5;
				secondValue = _menuButtonHeight * (newTabIndex + 1) - 1.5;
				firstProperty = Line.Y1Property;
				secondProperty = Line.Y2Property;
			}
			s_naviagtionAnimation.To = firstValue;
			s_naviagtionAnimation.BeginTime = TimeSpan.Zero;
			MenuLine.BeginAnimation(firstProperty, s_naviagtionAnimation);
			s_naviagtionAnimation.To = secondValue;
			s_naviagtionAnimation.BeginTime = TimeSpan.FromMilliseconds(250);
			MenuLine.BeginAnimation(secondProperty, s_naviagtionAnimation);
			_currentTabIndex = newTabIndex;
		}
		Navigate(newTabIndex switch
		{
			0 => new PlayTab(),
			1 => new ServersTab(),
			2 => s_gameOptionsTab ??= new GameOptionsTab(),
			3 => new DLCTab(),
			4 => new ModsTab(),
			5 => new LauncherSettingsTab(),
			_ => new AboutTab()
		});
	}
	/// <summary>Navigates to another tab by replacing the content of tab frame.</summary>
	/// <param name="newTab">The tab to navigate to.</param>
	public void Navigate(ContentControl newTab)
	{
		var currentTab = (ContentControl)TabFrame.Child;
		s_naviagtionAnimation.To = 0;
		s_naviagtionAnimation.BeginTime = TimeSpan.Zero;
		currentTab.BeginAnimation(OpacityProperty, s_naviagtionAnimation);
		if (currentTab is DLCTab)
			foreach (var dlc in ARK.DLC.List)
				dlc.Item = null;
		else if (currentTab is ModsTab)
			lock (Mod.List)
				foreach (var mod in Mod.List)
					mod.Item = null;
		Task.Delay(250).ContinueWith(t => Dispatcher.Invoke(delegate
		{
			newTab.Opacity = 0;
			TabFrame.Child = newTab;
			s_naviagtionAnimation.To = 1;
			newTab.BeginAnimation(OpacityProperty, s_naviagtionAnimation);
		}));
	}
	protected override void StateChangedHandler(object? sender, EventArgs e)
	{
		base.StateChangedHandler(sender, e);
		if (WindowState == WindowState.Normal)
		{
			MenuBorder.CornerRadius = new(0, 0, 0, 8);
			FrameBorder.CornerRadius = new(0, 0, 8, 0);
		}
		else if (WindowState == WindowState.Maximized)
		{
			MenuBorder.CornerRadius = new(0);
			FrameBorder.CornerRadius = new(0);
		}
	}
	/// <summary>Represents GitHub release JSON object, with tag_name left as the only necessary field.</summary>
	readonly record struct Release
	{
		[JsonPropertyName("tag_name")]
		public string? TagName { get; init; }
	}
}