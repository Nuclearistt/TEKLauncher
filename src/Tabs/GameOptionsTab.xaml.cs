using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shell;
using TEKLauncher.Controls;
using TEKLauncher.Steam;
using TEKLauncher.Windows;

namespace TEKLauncher.Tabs;

/// <summary>Tab that provides game-related options.</summary>
partial class GameOptionsTab : ContentControl
{
    /// <summary>Indicator of Steam client task's current stage.</summary>
    StageIndicator? _currentStage;
    /// <summary>Indicator of Steam client task's current sub-stage.</summary>
    StageIndicator? _currentSubStage;
    /// <summary>Handlers for dowloader and Steam client events.</summary>
    readonly EventHandlers _eventHandlers;
    /// <summary>Taskbar item info of main window.</summary>
    readonly TaskbarItemInfo _taskbarItemInfo;
    /// <summary>Cancellation token source for pausing/cancelling Steam client task.</summary>
    static CancellationTokenSource s_cts = new();
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
            StartValidation = () => Dispatcher.Invoke(delegate
            {
                Counters.Visibility = Visibility.Visible;
                FilesUpToDate.Text = FilesOutdated.Text = FilesMissing.Text = "0";
            }),
            PrepareProgress = (mode, total) => Dispatcher.Invoke(() => ProgressBar.Initialize(mode, total)),
            UpdateCounters = (missing, outdated, upToDate) => Dispatcher.InvokeAsync(delegate
            {
                FilesMissing.Text = missing.ToString();
                FilesOutdated.Text = outdated.ToString();
                FilesUpToDate.Text = upToDate.ToString();
            }),
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
            }),
            SetStage = (textCode, isSubStage) => Dispatcher.Invoke(delegate
            {
                if (isSubStage)
                {
                    _currentSubStage?.Finish(true);
                    _currentSubStage = new(textCode) { Margin = new(20, 0, 0, 0) };
                    Stages.Children.Add(_currentSubStage);
                }
                else
                {
                    _currentSubStage?.Finish(true);
                    _currentSubStage = null;
                    _currentStage?.Finish(true);
                    _currentStage = new(textCode);
                    Stages.Children.Add(_currentStage);
                }
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
    /// <summary>Executes Steam client tasks and catches its exceptions.</summary>
    /// <param name="obj">Thread parameter, for this method it's a <see cref="bool"/> that indicates whether validation should be performed.</param>
    /// <exception cref="AggregateException">And error occurred in Steam client task.</exception>
    void TaskProcedure(object? obj)
    {
        var tasks = Tasks.ReserveDiskSpace | Tasks.Download | Tasks.Install;
        tasks |= ((bool)obj!) ? Tasks.Validate : Tasks.GetUpdateData;
        try
        {
            Client.RunTasks(346111, tasks, _eventHandlers, s_cts.Token);
            Dispatcher.Invoke(delegate
            {
                _taskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                _currentSubStage?.Finish(true);
                _currentStage?.Finish(true);
                SwitchButtons(true, true);
            });
            string? version = Game.Version;
            bool match = (Client.CurrentManifestIds.TryGetValue(new(346111), out ulong id) ? id : 0) == Client.DepotManifestIds[346111];
            Dispatcher.Invoke(delegate
            {
                var gameVersion = ((MainWindow)Application.Current.MainWindow).GameVersion;
                gameVersion.Text = version ?? LocManager.GetString(match ? LocCode.Latest : LocCode.Outdated);
                gameVersion.Foreground = match ? new SolidColorBrush(Color.FromRgb(0x0A, 0xA6, 0x3E)) : Brushes.Yellow;
            });
        }
        catch (OperationCanceledException)
        {
            _eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.OperationPaused), 1);
            Dispatcher.Invoke(delegate
            {
                _taskbarItemInfo.ProgressState = TaskbarItemProgressState.Paused;
                _currentSubStage?.Finish(false);
                _currentStage?.Finish(false);
                SwitchButtons(true, true);
            });
        }
        catch (Exception e)
        {
            bool steamException = e is SteamException;
            if (steamException)
                _eventHandlers.SetStatus?.Invoke(e.Message, 2);
            else
                File.WriteAllText($@"{App.AppDataFolder}\SteamClientException.txt", e.ToString());
            Dispatcher.Invoke(delegate
            {
                _taskbarItemInfo.ProgressState = TaskbarItemProgressState.Error;
                _currentSubStage?.Finish(false);
                _currentStage?.Finish(false);
                SwitchButtons(true, true);
                if (!steamException)
                    new FatalErrorWindow(e).ShowDialog();
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
            s_cts.Cancel();
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
        if (IsSteamTaskActive)
            return;
        ExpandableBlock.IsExpanded = ExpandableBlock.IsEnabled = true;
        if (Game.IsRunning)
        {
            _eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.UpdateFailGameRunning), 2);
            return;
        }
        s_cts.Dispose();
        s_cts = new();
        _taskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
        Counters.Visibility = Visibility.Collapsed;
        Stages.Children.Clear();
        s_taskThread = new(TaskProcedure, 6291456);
        SwitchButtons(false, true);
        s_taskThread.Start(validate);
    }
    /// <summary>Force cancels Steam client task if it's running.</summary>
    public static void AbortTask()
    {
        if (IsSteamTaskActive)
            s_cts.Cancel();
    }
}