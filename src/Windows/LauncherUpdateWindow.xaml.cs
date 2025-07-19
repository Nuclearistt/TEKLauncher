﻿using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Shell;

namespace TEKLauncher.Windows;

/// <summary>Window that downloads and installs launcher updates.</summary>
partial class LauncherUpdateWindow : TEKWindow
{
    /// <summary>Initializes a new Launcher update window.</summary>
    public LauncherUpdateWindow() => InitializeComponent();
    /// <summary>Performs launcher update procedure.</summary>
    async void LoadedHandler(object sender, RoutedEventArgs e)
    {
        TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
        using var currentProcess = Process.GetCurrentProcess();
        string path = currentProcess!.MainModule!.FileName!;
        string newPath = string.Concat(path, ".new");
        var eventHandlers = new EventHandlers
        {
            PrepareProgress = (mode, total) => Dispatcher.Invoke(() => ProgressBar.Initialize(mode, total)),
            UpdateProgress = increment => Dispatcher.Invoke(delegate
            {
                ProgressBar.Update(increment);
                TaskbarItemInfo.ProgressValue = ProgressBar.Ratio;
            })
        };
        if (await Downloader.DownloadFileAsync(newPath, eventHandlers, "https://teknology-hub.com/software/tek-launcher/releases/latest/win-x86_64-static/tek-launcher.exe", "https://de.teknology-hub.com/software/tek-launcher/releases/latest/win-x86_64-static/tek-launcher.exe", "https://github.com/Nuclearistt/TEKLauncher/releases/latest/download/TEKLauncher.exe"))
        {
            File.Move(path, string.Concat(path, ".old"), true);
            File.Move(newPath, path);
            IPC.Dispose();
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            Application.Current.Shutdown();
        }
        else
            Dispatcher.Invoke(delegate
            {
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Error;
                Status.Foreground = new SolidColorBrush(Color.FromRgb(0x9E, 0x23, 0x13));
                Status.Text = LocManager.GetString(LocCode.LauncherUpdateFail);
            });
    }
}