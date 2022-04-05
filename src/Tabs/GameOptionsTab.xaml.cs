﻿using System.Diagnostics;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shell;
using Microsoft.Win32;
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
        string file = $@"{Game.Path}\Engine\Config\BaseScalability.ini";
        if (File.Exists(file))
        {
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
                if (lines[i].StartsWith("r.BloomQuality"))
                    lines[i] = "r.BloomQuality=1";
            File.WriteAllLines(file, lines);
        }
        Notifications.Add(LocManager.GetString(LocCode.FixBloomSuccess), "NSuccess");
    }
    /// <summary>Installs game's software requirements, that consist of MSVCR 2010, MSVCR 2012, MSVCR 2013 and DirectX.</summary>
    async void InstallRequirements(object sender, RoutedEventArgs e)
    {
        var baseVCKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\VisualStudio");
        if (baseVCKey is not null &&
            baseVCKey.OpenSubKey(@"10.0\VC\VCRedist\x64") is not null &&
            baseVCKey.OpenSubKey(@"10.0\VC\VCRedist\x86") is not null &&
            baseVCKey.OpenSubKey(@"11.0\VC\Runtimes\x64") is not null &&
            baseVCKey.OpenSubKey(@"11.0\VC\Runtimes\x86") is not null &&
            baseVCKey.OpenSubKey(@"12.0\VC\Runtimes\x64") is not null &&
            baseVCKey.OpenSubKey(@"12.0\VC\Runtimes\x86") is not null &&
            (((int?)Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\DirectX")?.GetValue("MaxFeatureLevel")) ?? 0) >= 0xA000)
        {
            Notifications.Add(LocManager.GetString(LocCode.RequirementsAlreadyInstalled), "NSuccess");
            return;
        }
        SwitchButtons(false, false);
        ExpandableBlock.IsExpanded = ExpandableBlock.IsEnabled = true;
        string archivePath = $@"{App.AppDataFolder}\Dw_CommonRedist.br";
        _eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.Downloading), 0);
        bool success = await Downloader.DownloadFileAsync(archivePath, _eventHandlers, "http://95.217.84.23/files/Ark/Extra/CommonRedist.br");
        if (!success)
        {
            Dispatcher.Invoke(delegate
            {
                SwitchButtons(true, false);
                _taskbarItemInfo.ProgressState = TaskbarItemProgressState.Error;
                _eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.DownloadFailed), 2);
            });
            return;
        }
        _eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.ExtractingArchive), 0);
        string baseDir = $@"{App.AppDataFolder}\Dw_CommonRedist\";
        success = await Task.Run(() => BrotliArchive.Decompress(archivePath, baseDir, _eventHandlers));
        if (!success)
        {
            File.Delete(archivePath);
            Dispatcher.Invoke(delegate
            {
                SwitchButtons(true, false);
                _taskbarItemInfo.ProgressState = TaskbarItemProgressState.Error;
                _eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.ArchiveCorrupted), 2);
            });
            return;
        }
        _eventHandlers.PrepareProgress?.Invoke(false, 7);
        var startInfo = new ProcessStartInfo(string.Concat(baseDir, @"DirectX\DXSETUP.exe"), "/silent") { UseShellExecute = true };
        _eventHandlers.SetStatus?.Invoke(string.Format(LocManager.GetString(LocCode.InstallingRequirement), "DirectX"), 0);
        await (Process.Start(startInfo)?.WaitForExitAsync() ?? Task.CompletedTask);
        _eventHandlers.UpdateProgress?.Invoke(1);
        startInfo.FileName = string.Concat(baseDir, @"MSVCP\2010\vcredist_x64.exe");
        startInfo.Arguments = "/q /norestart";
        _eventHandlers.SetStatus?.Invoke(string.Format(LocManager.GetString(LocCode.InstallingRequirement), "Microsoft Visual C++ Redist 2010 x64"), 0);
        await (Process.Start(startInfo)?.WaitForExitAsync() ?? Task.CompletedTask);
        _eventHandlers.UpdateProgress?.Invoke(1);
        startInfo.FileName = string.Concat(baseDir, @"MSVCP\2010\vcredist_x86.exe");
        _eventHandlers.SetStatus?.Invoke(string.Format(LocManager.GetString(LocCode.InstallingRequirement), "Microsoft Visual C++ Redist 2010 x86"), 0);
        await (Process.Start(startInfo)?.WaitForExitAsync() ?? Task.CompletedTask);
        _eventHandlers.UpdateProgress?.Invoke(1);
        startInfo.FileName = string.Concat(baseDir, @"MSVCP\2012\vcredist_x64.exe");
        startInfo.Arguments = "/install /quiet /norestart";
        _eventHandlers.SetStatus?.Invoke(string.Format(LocManager.GetString(LocCode.InstallingRequirement), "Microsoft Visual C++ Redist 2012 x64"), 0);
        await (Process.Start(startInfo)?.WaitForExitAsync() ?? Task.CompletedTask);
        _eventHandlers.UpdateProgress?.Invoke(1);
        startInfo.FileName = string.Concat(baseDir, @"MSVCP\2012\vcredist_x86.exe");
        _eventHandlers.SetStatus?.Invoke(string.Format(LocManager.GetString(LocCode.InstallingRequirement), "Microsoft Visual C++ Redist 2012 x86"), 0);
        await (Process.Start(startInfo)?.WaitForExitAsync() ?? Task.CompletedTask);
        _eventHandlers.UpdateProgress?.Invoke(1);
        startInfo.FileName = string.Concat(baseDir, @"MSVCP\2013\vcredist_x64.exe");
        _eventHandlers.SetStatus?.Invoke(string.Format(LocManager.GetString(LocCode.InstallingRequirement), "Microsoft Visual C++ Redist 2013 x64"), 0);
        await (Process.Start(startInfo)?.WaitForExitAsync() ?? Task.CompletedTask);
        _eventHandlers.UpdateProgress?.Invoke(1);
        startInfo.FileName = string.Concat(baseDir, @"MSVCP\2013\vcredist_x86.exe");
        _eventHandlers.SetStatus?.Invoke(string.Format(LocManager.GetString(LocCode.InstallingRequirement), "Microsoft Visual C++ Redist 2013 x86"), 0);
        await (Process.Start(startInfo)?.WaitForExitAsync() ?? Task.CompletedTask);
        _eventHandlers.UpdateProgress?.Invoke(1);
        await Task.Run(() => Directory.Delete($@"{App.AppDataFolder}\Dw_CommonRedist", true));
        Dispatcher.Invoke(delegate
        {
            SwitchButtons(true, false);
            _taskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
            _eventHandlers.SetStatus?.Invoke(LocManager.GetString(LocCode.InstallRequirementsSuccess), 1);
        });
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
        InstallRequirementsButton.IsEnabled = ValidateButton.IsEnabled = UnlockSkinsButton.IsEnabled = newState;
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
            bool? hashMatch = null;
            if (HashManager.Load())
            {
                Span<byte> hash = stackalloc byte[20];
                using var stream = File.OpenRead(Game.ExePath);
                SHA1.ComputeHash(stream, hash);
                hashMatch = new Hash.StackHash(hash) == HashManager.GameHash;
            }
            Dispatcher.Invoke(delegate
            {
                var gameVersion = ((MainWindow)Application.Current.MainWindow).GameVersion;
                gameVersion.Text = version ?? LocManager.GetString(hashMatch switch
                {
                    null => LocCode.NA,
                    false => LocCode.Outdated,
                    true => LocCode.Latest
                });
                gameVersion.Foreground = hashMatch switch
                {
                    null => new SolidColorBrush(Color.FromRgb(0x9F, 0xD6, 0xD2)),
                    false => Brushes.Yellow,
                    true => new SolidColorBrush(Color.FromRgb(0x0A, 0xA6, 0x3E))
                };
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
        catch (SteamException e)
        {
            _eventHandlers.SetStatus?.Invoke(e.Message, 2);
            Dispatcher.Invoke(delegate
            {
                _taskbarItemInfo.ProgressState = TaskbarItemProgressState.Error;
                _currentSubStage?.Finish(false);
                _currentStage?.Finish(false);
                SwitchButtons(true, true);
            });
        }
        catch (Exception e) { Dispatcher.Invoke(() => throw new AggregateException(e)); }
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
        bool success = await Downloader.DownloadFileAsync(file, _eventHandlers, "http://95.217.84.23/files/Ark/Extra/PlayerLocalData.arkprofile", "https://drive.google.com/uc?export=download&id=1YsuoGqf-XOvdg5oneuoPDOVeVN8uRkRF");
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
            if (Steam.App.IsARKPurchased && !Messages.ShowOptions("Warning", LocManager.GetString(LocCode.UpdateSteamGameWarning)))
                return;
            RunTask(false);
        }
    }
    /// <summary>Enables or disables global fonts in the game.</summary>
    void UpdateUseGlobalFonts(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;
        string mixedFolder = $@"{Game.Path}\ShooterGame\Content\Localization\Game\mixed";
        string currentLocFolder = $@"{Game.Path}\ShooterGame\Content\Localization\Game\{Game.CultureCodes[Game.Language]}";
        string currentArchive = $@"{currentLocFolder}\ShooterGame.archive";
        string currentLocRes = $@"{currentLocFolder}\ShooterGame.locres";
        string mixedArchive = $@"{mixedFolder}\ShooterGame.archive";
        string mixedLocRes = $@"{mixedFolder}\ShooterGame.locres";
        string archiveBak = string.Concat(mixedArchive, ".bak");
        string locResBak = string.Concat(mixedLocRes, ".bak");
        Game.UseGlobalFonts = !Game.UseGlobalFonts;
        if (!Directory.Exists(mixedFolder))
            return;
        if (Game.UseGlobalFonts)
        {
            if (File.Exists(currentArchive))
            {
                if (File.Exists(mixedArchive))
                    File.Move(mixedArchive, archiveBak, true);
                File.Copy(currentArchive, mixedArchive, true);
            }
            if (File.Exists(currentLocRes))
            {
                if (File.Exists(mixedLocRes))
                    File.Move(mixedLocRes, locResBak, true);
                File.Copy(currentLocRes, mixedLocRes, true);
            }
        }
        else
        {
            if (File.Exists(archiveBak))
                File.Move(archiveBak, mixedArchive, true);
            if (File.Exists(locResBak))
                File.Move(locResBak, mixedLocRes, true);
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