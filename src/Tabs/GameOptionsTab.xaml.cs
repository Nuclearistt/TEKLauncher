using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shell;
using TEKLauncher.Controls;
using TEKLauncher.Windows;

namespace TEKLauncher.Tabs;

/// <summary>Tab that provides game-related options.</summary>
partial class GameOptionsTab : ContentControl
{
    /// <summary>Indicator of Steam client task's current stage.</summary>
    StageIndicator? _currentStage;
    /// <summary>Handlers for dowloader and Steam client events.</summary>
    readonly EventHandlers _eventHandlers;
    /// <summary>Taskbar item info of main window.</summary>
    readonly TaskbarItemInfo _taskbarItemInfo;
    static unsafe TEKSteamClient.AmItemDesc *s_desc = null;
    /// <summary>Steam client task thread.</summary>
    static Thread? s_taskThread;
    /// <summary>Initializes a new instance of Game options tab.</summary>
    public GameOptionsTab()
    {
        InitializeComponent();
        foreach (StackPanel stack in LaunchParametersGrid.Children)
            foreach (var child in stack.Children)
                if (child is CheckBox checkBox)
                {
                    string parameter = (string)checkBox.Tag;
                    if (Game.LaunchParameters.Contains(parameter))
                        checkBox.IsChecked = true;
                    checkBox.Checked += CheckParameter;
                    checkBox.Unchecked += UncheckParameter;
                }
        CustomLaunchParameters.Text = string.Join(' ', Game.LaunchParameters.FindAll(p => Array.IndexOf(Game.StandardLaunchParameters, p) == -1));
        _taskbarItemInfo = Application.Current.MainWindow.TaskbarItemInfo;
        _eventHandlers = new()
        {
            PrepareProgress = (mode, total) => Dispatcher.Invoke(() => ProgressBar.Initialize(mode, total)),
            UpdateProgress = increment => Dispatcher.Invoke(delegate
            {
                ProgressBar.Update(increment);
                _taskbarItemInfo.ProgressValue = ProgressBar.Ratio;
            }),
            SetStatus = (text, color) => Dispatcher.Invoke(delegate
            {
                Status.Text = text;
                Status.Foreground = new SolidColorBrush(color switch
                {
                    0 => Colors.Yellow,
                    1 => Color.FromRgb(0x0A, 0xA6, 0x3E),
                    2 => Color.FromRgb(0x9E, 0x23, 0x13),
                    _ => default
                });
            })
        };
    }
    /// <summary>Gets a value that indicates whether there is an active Steam client task initiated by this tab.</summary>
    public static bool IsSteamTaskActive => s_taskThread?.IsAlive ?? false;
    /// <summary>Enables launch parameter that belongs to sender checkbox.</summary>
    void CheckParameter(object sender, RoutedEventArgs e) => Game.LaunchParameters.Add((string)((CheckBox)sender).Tag);
    /// <summary>"Fixes" bloom quality ingame by decreasing its value in base scalability configuration file.</summary>
    void FixBloom(object sender, RoutedEventArgs e)
    {
        if (Game.IsRunning)
        {
            Notifications.Add(LocManager.GetString(LocCode.FixBloomFail), "NError");
            return;
        }
        string file = $@"{Game.Path}\ShooterGame\Saved\Config\WindowsNoEditor\Scalability.ini";
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        using var writer = new StreamWriter(file);
        for (int i = 0; i < 4; i++)
        {
            string entry = $"[PostProcessQuality@{i}]\r\nr.BloomQuality=1";
            if (i < 3)
                entry += "\r\n";
            writer.Write(entry);
        }
        Notifications.Add(LocManager.GetString(LocCode.FixBloomSuccess), "NSuccess");
    }
    /// <summary>Updates <see cref="Game.LaunchParameters"/> with the parameters specified in <see cref="CustomLaunchParameters"/>.</summary>
    void LostFocusHandler(object sender, RoutedEventArgs e)
    {
        Game.LaunchParameters.RemoveAll(p => Array.IndexOf(Game.StandardLaunchParameters, p) == -1);
        if (string.IsNullOrEmpty(CustomLaunchParameters.Text))
            return;
        foreach (string parameter in CustomLaunchParameters.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (!Game.LaunchParameters.Contains(parameter))
                Game.LaunchParameters.Add(parameter);
    }
    /// <summary>Switches the enabled state of buttons whose options involve downloads.</summary>
    /// <param name="newState">New enabled state of the buttons.</param>
    /// <param name="steamTask">Indicates whether switch is initiated by a Steam client task and Update/Pause state of the button should be determined.</param>
    void SwitchButtons(bool newState, bool steamTask)
    {
        ValidateButton.IsEnabled = UnlockSkinsButton.IsEnabled = newState;
        if (steamTask)
        {
            UpdatePauseButton.Content = LocManager.GetString(newState ? LocCode.Update : LocCode.Pause);
            UpdatePauseButton.Tag = FindResource(newState ? "Retry" : "Pause");
            UpdatePauseButton.IsEnabled = true;
        }
        else
            UpdatePauseButton.IsEnabled = newState;
    }
    static string? s_spcMsg = null;
    void UpdHandler(ref TEKSteamClient.AmItemDesc desc, TEKSteamClient.AmUpdType upd_mask)
    {
        if (upd_mask.HasFlag(TEKSteamClient.AmUpdType.DeltaCreated))
        {
			long diskFreeSpace = WinAPI.GetDiskFreeSpace($@"{Game.Path}\tek-sc-data") + 20971520; //Add 20 MB to take NTFS entries into account
            long reqSpace = TEKSteamClient.DeltaEstimateDiskSpace(desc.Job.Delta);
			if (diskFreeSpace < reqSpace)
            {
				s_spcMsg = string.Format(LocManager.GetString(LocCode.NotEnoughSpace), LocManager.BytesToString(reqSpace));
                TEKSteamClient.AppManager.PauseJob(ref desc);
			}
        }
		if (upd_mask.HasFlag(TEKSteamClient.AmUpdType.Stage))
		{
            var stage = desc.Job.Stage;
            Dispatcher.Invoke(() =>
			{
                LocCode code = LocCode.NA;
				switch (stage)
				{
					case TEKSteamClient.AmJobStage.FetchingData:
                        ProgressBar.Reset(Controls.ProgressBar.Mode.None);
                        code = LocCode.FetchingData;
						break;
					case TEKSteamClient.AmJobStage.DwManifest:
                        ProgressBar.Reset(Controls.ProgressBar.Mode.Binary);
                        code = LocCode.DownloadingManifest;
						break;
					case TEKSteamClient.AmJobStage.DwPatch:
                        ProgressBar.Reset(Controls.ProgressBar.Mode.Binary);
                        code = LocCode.DownloadingPatch;
						break;
					case TEKSteamClient.AmJobStage.Verifying:
                        ProgressBar.Reset(Controls.ProgressBar.Mode.Percentage);
                        code = LocCode.Validating;
						break;
					case TEKSteamClient.AmJobStage.Downloading:
                        ProgressBar.Reset(Controls.ProgressBar.Mode.Binary);
                        code = LocCode.DownloadingFiles;
						break;
					case TEKSteamClient.AmJobStage.Pathcing:
                        ProgressBar.Reset(Controls.ProgressBar.Mode.Numbers);
                        code = LocCode.Patching;
						break;
					case TEKSteamClient.AmJobStage.Installing:
                        ProgressBar.Reset(Controls.ProgressBar.Mode.Numbers);
                        code = LocCode.InstallingFiles;
						break;
					case TEKSteamClient.AmJobStage.Deleting:
                        ProgressBar.Reset(Controls.ProgressBar.Mode.Numbers);
                        code = LocCode.Deleting;
						break;
					case TEKSteamClient.AmJobStage.Finalizing:
                        ProgressBar.Reset(Controls.ProgressBar.Mode.None);
                        code = LocCode.Finalizing;
						break;
				}
				Status.Text = LocManager.GetString(code);
				Status.Foreground = Brushes.Yellow;
				_currentStage?.Finish(true);
				_currentStage = new(code);
				Stages.Children.Add(_currentStage);
			});
		}
        if (upd_mask.HasFlag(TEKSteamClient.AmUpdType.Progress))
        {
            var current = desc.Job.Stage switch
            {
                TEKSteamClient.AmJobStage.Verifying
                or TEKSteamClient.AmJobStage.Downloading
                or TEKSteamClient.AmJobStage.Installing => Interlocked.Read(ref desc.Job.ProgressCurrent),
                _ => desc.Job.ProgressCurrent
            };
            var total = desc.Job.ProgressTotal;
			Dispatcher.Invoke(delegate
			{
				ProgressBar.Update(current, total);
				_taskbarItemInfo.ProgressValue = ProgressBar.Ratio;
			});

		}
	}
    /// <summary>Executes Steam client tasks and catches its exceptions.</summary>
    /// <param name="obj">Thread parameter, for this method it's a <see cref="bool"/> that indicates whether validation should be performed.</param>
    /// <exception cref="AggregateException">And error occurred in Steam client task.</exception>
    unsafe void TaskProcedure(object? obj)
    {
        bool forceVerify = (bool)obj!;
        var itemId = new TEKSteamClient.ItemId { AppId = 346110, DepotId = 346111, WorkshopItemId = 0 };
        var res = TEKSteamClient.AppMng!.RunJob(in itemId, Settings.PreAquatica ? 8075379529797638112ul : 0, forceVerify, UpdHandler, out s_desc);
        if (res.Primary == 65)
        {
			Dispatcher.Invoke(delegate
			{
                if (s_spcMsg is null)
                {
					ProgressBar.Reset(Controls.ProgressBar.Mode.Done);
					Status.Text = LocManager.GetString(LocCode.OperationPaused);
					Status.Foreground = new SolidColorBrush(Color.FromRgb(0x0A, 0xA6, 0x3E));
					_taskbarItemInfo.ProgressState = TaskbarItemProgressState.Paused;

				}
				else
                {
					ProgressBar.Reset(Controls.ProgressBar.Mode.None);
				    Status.Text = s_spcMsg;
                    s_spcMsg = null;
				    Status.Foreground = new SolidColorBrush(Color.FromRgb(0x9E, 0x23, 0x13));
				    _taskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
				}
				_currentStage?.Finish(false);
				SwitchButtons(true, true);
			});
        }
        else if (!res.Success && res.Primary != 82)
        {
			Dispatcher.Invoke(delegate
			{
                ProgressBar.Reset(Controls.ProgressBar.Mode.Done);
				Status.Text = res.Message;
                if (res.Uri != 0)
                    Marshal.FreeHGlobal(res.Uri);
				Status.Foreground = new SolidColorBrush(Color.FromRgb(0x9E, 0x23, 0x13));
				_taskbarItemInfo.ProgressState = TaskbarItemProgressState.Error;
				_currentStage?.Finish(false);
				SwitchButtons(true, true);
			});
		}
        else
        {
			Dispatcher.Invoke(delegate
			{
                ProgressBar.Reset(Controls.ProgressBar.Mode.Done);
				Status.Text = res.Primary == 82 ? string.Format(LocManager.GetString(LocCode.AlreadyUpToDate), "ARK") : LocManager.GetString(LocCode.UpdateFinished);
				Status.Foreground = new SolidColorBrush(Color.FromRgb(0x0A, 0xA6, 0x3E));
				_taskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
				_currentStage?.Finish(true);
				SwitchButtons(true, true);
			    bool updAvailable = Settings.PreAquatica ? s_desc->CurrentManifestId != 8075379529797638112 : s_desc->Status.HasFlag(TEKSteamClient.AmItemStatus.UpdAvailable);
				var gameVersion = ((MainWindow)Application.Current.MainWindow).GameVersion;
				gameVersion.Text = LocManager.GetString(updAvailable ? LocCode.Outdated : LocCode.Latest);
				gameVersion.Foreground = updAvailable ? Brushes.Yellow : new SolidColorBrush(Color.FromRgb(0x0A, 0xA6, 0x3E));
			});
		}
    }
    /// <summary>Disables launch parameter that belongs to sender checkbox.</summary>
    void UncheckParameter(object sender, RoutedEventArgs e) => Game.LaunchParameters.Remove((string)((CheckBox)sender).Tag);
    /// <summary>Downloads and installs the PlayerLocalData.arkprofile file with all skins and haircuts unlocked.</summary>
    async void UnlockSkins(object sender, RoutedEventArgs e)
    {
        if (Game.IsRunning)
        {
            Notifications.Add(LocManager.GetString(LocCode.UnlockSkinsFail), "NError");
            return;
        }
        SwitchButtons(false, false);
        ExpandableBlock.IsExpanded = ExpandableBlock.IsEnabled = true;
        string file = $@"{App.AppDataFolder}\Dw_PlayerLocalData.arkprofile";
        _eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.Downloading), 0);
        bool success = await Downloader.DownloadFileAsync(file, _eventHandlers, "https://nuclearist.ru/static/teklauncher/PlayerLocalData.arkprofile", "https://drive.google.com/uc?export=download&id=1YsuoGqf-XOvdg5oneuoPDOVeVN8uRkRF");
        Dispatcher.Invoke(delegate
        {
            SwitchButtons(true, false);
            if (success)
            {
                Directory.CreateDirectory($@"{Game.Path}\ShooterGame\Saved\LocalProfiles");
                File.Move(file, $@"{Game.Path}\ShooterGame\Saved\LocalProfiles\PlayerLocalData.arkprofile", true);
                _taskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                _eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.UnlockSkinsSuccess), 1);
            }
            else
            {
                _taskbarItemInfo.ProgressState = TaskbarItemProgressState.Error;
                _eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.DownloadFailed), 2);
            }
        });
    }
    /// <summary>Toggles high process priority setting.</summary>
    void UpdateHighProcessPriority(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;
        Game.HighProcessPriority = !Game.HighProcessPriority;
    }
    /// <summary>Initiates update of base game or pauses Steam client task.</summary>
    void UpdatePause(object sender, RoutedEventArgs e)
    {
        if ((string)UpdatePauseButton.Content == LocManager.GetString(LocCode.Pause))
        {
            UpdatePauseButton.IsEnabled = false;
            unsafe
            {
                TEKSteamClient.AppManager.PauseJob(ref Unsafe.AsRef<TEKSteamClient.AmItemDesc>(s_desc));
            }
		}
        else
        {
            if (Steam.App.CurrentUserStatus.GameStatus == Game.Status.OwnedAndInstalled && !Messages.ShowOptions("Warning", LocManager.GetString(LocCode.UpdateSteamGameWarning)))
                return;
            RunTask(false);
        }
    }
    /// <summary>Initiates validation of base game files.</summary>
    void Validate(object sender, RoutedEventArgs e) => RunTask(true);
    /// <summary>Runs Steam client tasks.</summary>
    /// <param name="validate">Indicates whether validation should be preferred over update.</param>
    public void RunTask(bool validate)
    {
        if (TEKSteamClient.Ctx == null)
        {
            Messages.Show("Error", "tek-steamclient library hasn't been downloaded yet, try again later");
            return;
        }
        if (IsSteamTaskActive)
            return;
        ExpandableBlock.IsExpanded = ExpandableBlock.IsEnabled = true;
        if (Game.IsRunning)
        {
            _eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.UpdateFailGameRunning), 2);
            return;
        }
        _taskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
        Stages.Children.Clear();
        s_taskThread = new(TaskProcedure);
        SwitchButtons(false, true);
        s_taskThread.Start(validate);
    }
    /// <summary>Force cancels Steam client task if it's running.</summary>
    public static unsafe void AbortTask()
    {
        if (IsSteamTaskActive)
            TEKSteamClient.AppManager.PauseJob(ref Unsafe.AsRef<TEKSteamClient.AmItemDesc>(s_desc));
    }
}