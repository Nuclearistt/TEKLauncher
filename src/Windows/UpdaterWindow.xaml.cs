using System.ComponentModel;
using System.Threading;
using System.Windows.Media;
using System.Windows.Shell;
using TEKLauncher.Controls;
using TEKLauncher.Steam;

namespace TEKLauncher.Windows;

/// <summary>Window that represents a Steam client task for a DLC or a mod.</summary>
partial class UpdaterWindow : TEKWindow
{
    /// <summary>New status of DLC/mod that will be set when the window is closed; -1 means that last status should be kept.</summary>
    int _newStatus = -1;
    /// <summary>Cancellation token source for pausing/cancelling Steam client task.</summary>
    CancellationTokenSource _cts = new();
    /// <summary>Indicator of Steam client task's current stage.</summary>
    StageIndicator? _currentStage;
    /// <summary>Indicator of Steam client task's current sub-stage.</summary>
    StageIndicator? _currentSubStage;
    /// <summary>Steam client task thread.</summary>
    Thread _taskThread;
    /// <summary>Indicates whether validation should be preferred over update.</summary>
    readonly bool _validate;
    /// <summary>The last status that DLC/mod had before creating the window.</summary>
    readonly int _lastStatus;
    /// <summary>Handlers for Steam client events.</summary>
    readonly EventHandlers _eventHandlers;
    /// <summary>Initializes a new Updater window for specified item.</summary>
    /// <param name="item">The item (DLC, mod or list of mods) to be processed by the window's Steam client task.</param>
    /// <param name="validate">Indicates whether validation should be preferred over update.</param>
    UpdaterWindow(object item, bool validate)
    {
        InitializeComponent();
        _validate = validate;
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
                TaskbarItemInfo.ProgressValue = ProgressBar.Ratio;
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
        _taskThread = new(TaskProcedure, 6291456);
        Item = item;
    }
    /// <summary>Initializes a new Updater window for specified DLC.</summary>
    /// <param name="dlc">The DLC to be processed by the window's Steam client task.</param>
    /// <param name="validate">Indicates whether validation should be preferred over update.</param>
    internal UpdaterWindow(DLC dlc, bool validate) : this((object)dlc, validate)
    {
        _lastStatus = (int)dlc.CurrentStatus;
        dlc.CurrentStatus = DLC.Status.Updating;
        Title = string.Format(LocManager.GetString(LocCode.SteamUpdater), dlc.Name);
    }
    /// <summary>Initializes a new Updater window for specified mod.</summary>
    /// <param name="modDetails">Details of the mod to be processed by the window's Steam client task.</param>
    /// <param name="validate">Indicates whether validation should be preferred over update.</param>
    internal UpdaterWindow(in Mod.ModDetails modDetails, bool validate) : this((object)modDetails, validate)
    {
        lock (Mod.List)
        {
            ulong id = modDetails.Id;
            var mod = Mod.List.Find(m => m.Id == id);
            if (mod is not null)
            {
                _lastStatus = (int)mod.CurrentStatus;
                mod.CurrentStatus = Mod.Status.Updating;
            }
        }
        Title = string.Format(LocManager.GetString(LocCode.SteamUpdater), modDetails.Name);
    }
    /// <summary>Initializes a new Updater window for validating specified list of mods.</summary>
    /// <param name="modIds">Array of IDs of the mods that will be validated by the window.</param>
    internal UpdaterWindow(ulong[] modIds) : this(modIds, true)
    {
        if (modIds.Length == 0)
        {
            Close();
            return;
        }
        _newStatus = 0; //_newStatus will be used as index into modDetails
    }
    ~UpdaterWindow() => _cts.Dispose();
    /// <summary>Gets a value that indicates whether there is an active Steam client task initiated by this window.</summary>
    public bool IsSteamTaskActive => _taskThread.IsAlive;
    /// <summary>The item (DLC, mod or list of mods) to be processed by the window's Steam client task.</summary>
    public object Item { get; private set; }
    /// <summary>Checks whether the window is eligible for closing and sets new DLC/mod status if it is.</summary>
    void ClosingHandler(object sender, CancelEventArgs e)
    {
        if (_taskThread.IsAlive)
        {
            if (App.ShuttingDown)
                _cts.Cancel();
            else
            {
                e.Cancel = true;
                Messages.Show("Warning", LocManager.GetString(LocCode.PauseRequired));
                return;
            }
        }
        else if (Item is DLC dlc)
            dlc.CurrentStatus = _newStatus == -1 ? (DLC.Status)_lastStatus : (DLC.Status)_newStatus;
        else if (Item is Mod.ModDetails details)
            lock (Mod.List)
            {
                var mod = Mod.List.Find(m => m.Id == details.Id);
                if (mod is not null)
                    mod.CurrentStatus = _newStatus == -1 ? (Mod.Status)_lastStatus : (Mod.Status)_newStatus;
            }    
        _eventHandlers.StartValidation = null;
        _eventHandlers.PrepareProgress = null;
        _eventHandlers.UpdateCounters = null;
        _eventHandlers.UpdateProgress = null;
        _eventHandlers.SetStatus = null;
        _eventHandlers.SetStage = null;
    }
    /// <summary>Perform game running check and starts the task thread.</summary>
    void LoadedHandler(object sender, RoutedEventArgs e)
    {
        if (Game.IsRunning)
        {
            _eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.UpdateFailGameRunning), 2);
            PauseRetryButton.Content = LocManager.GetString(LocCode.Retry);
            PauseRetryButton.Tag = FindResource("Retry");
        }
        else
        {
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            _taskThread.Start();
        }
    }
    /// <summary>Cancels or restarts Steam client task.</summary>
    void PauseRetry(object sender, RoutedEventArgs e)
    {
        if ((string)PauseRetryButton.Content == LocManager.GetString(LocCode.Pause))
        {
            PauseRetryButton.IsEnabled = false;
            _cts.Cancel();
        }
        else if (Game.IsRunning)
            _eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.UpdateFailGameRunning), 2);
        else
        {
            _cts = new();
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            Counters.Visibility = Visibility.Collapsed;
            Stages.Children.Clear();
            _taskThread = new(TaskProcedure, 6291456);
            PauseRetryButton.Content = LocManager.GetString(LocCode.Pause);
            PauseRetryButton.Tag = FindResource("Pause");
            _taskThread.Start();
        }
    }
    /// <summary>Executes Steam client tasks and catches its exceptions.</summary>
    /// <exception cref="AggregateException">And error occurred in Steam client task.</exception>
    void TaskProcedure()
    {
        uint depotId = Item is DLC dlc ? dlc.DepotId : 346110;
        var tasks = Tasks.ReserveDiskSpace | Tasks.Download | Tasks.Install;
        tasks |= _validate ? Tasks.Validate : Tasks.GetUpdateData;
        if (depotId == 346110)
            tasks |= Tasks.UnpackMod | Tasks.FinishModInstall;
        try
        {
            if (Item is ulong[] modIds)
            {
                _eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.RequestingModDetails), 0);
                var details = Steam.CM.Client.GetModDetails(modIds);
                if (details.Length == 0)
                    throw new SteamException(LocManager.GetString(LocCode.RequestingModDetailsFail));
                Item = details;
            }
            if (Item is Mod.ModDetails[] array)
            {
                for (; _newStatus < array.Length; _newStatus++)
                {
                    var details = array[_newStatus];
                    _cts = new();
                    Dispatcher.Invoke(delegate
                    {
                        Title = string.Format(LocManager.GetString(LocCode.SteamUpdater), $"{details.Name} [{_newStatus + 1}/{array.Length}]");
                        TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                        Counters.Visibility = Visibility.Collapsed;
                        Stages.Children.Clear();
                    });
                    Client.RunTasks(346110, tasks, _eventHandlers, _cts.Token, in details);
                }
                Dispatcher.Invoke(delegate
                {
                    TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                    _currentSubStage?.Finish(true);
                    _currentStage?.Finish(true);
                    PauseRetryButton.IsEnabled = false;
                });
            }
            else
            {
                var modDetails = Item is Mod.ModDetails details ? details : default;
                Client.RunTasks(depotId, tasks, _eventHandlers, _cts.Token, in modDetails);
                Dispatcher.Invoke(delegate
                {
                    TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                    _currentSubStage?.Finish(true);
                    _currentStage?.Finish(true);
                    PauseRetryButton.IsEnabled = false;
                });
                _newStatus = depotId == 346110 ? (int)Mod.Status.Installed : (int)DLC.Status.Installed;
            }
        }
        catch (OperationCanceledException)
        {
            _eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.OperationPaused), 1);
            Dispatcher.Invoke(delegate
            {
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Paused;
                _currentSubStage?.Finish(false);
                _currentStage?.Finish(false);
                PauseRetryButton.IsEnabled = true;
                PauseRetryButton.Content = LocManager.GetString(LocCode.Retry);
                PauseRetryButton.Tag = FindResource("Retry");
            });
        }
        catch (SteamException e)
        {
            _eventHandlers.SetStatus?.Invoke(e.Message, 2);
            Dispatcher.Invoke(delegate
            {
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Error;
                _currentSubStage?.Finish(false);
                _currentStage?.Finish(false);
                PauseRetryButton.IsEnabled = true;
                PauseRetryButton.Content = LocManager.GetString(LocCode.Retry);
                PauseRetryButton.Tag = FindResource("Retry");
            });
        }
        catch (Exception e) { Dispatcher.Invoke(() => throw new AggregateException(e)); }
    }
    /// <summary>Force cancels Steam client task if it's running.</summary>
    public void AbortTask()
    {
        if (_taskThread.IsAlive)
            _cts.Cancel();
    }
}