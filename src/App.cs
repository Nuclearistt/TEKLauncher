global using System;
global using System.Collections.Generic;
global using System.Globalization;
global using System.IO;
global using System.Net;
global using System.Text;
global using System.Text.Json;
global using System.Threading.Tasks;
global using System.Windows;
global using TEKLauncher.ARK;
global using TEKLauncher.Data;
global using TEKLauncher.UI;
global using TEKLauncher.Utils;
using System.Diagnostics;
using System.IO.Pipes;
using System.Media;
using System.Reflection;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using TEKLauncher.Servers;
using TEKLauncher.Tabs;
using TEKLauncher.Windows;

namespace TEKLauncher;

/// <summary>Main class of the application.</summary>
partial class App : Application
{
    /// <summary>Named pipe server that prevents running another launcher instance and is used by <see cref="Steam.TEKInjectorInterop"/>.</summary>
    readonly NamedPipeServerStream? _pipeServer;
    /// <summary>Path to launcher's folder in AppData\Roaming.</summary>
    public static readonly string AppDataFolder = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\TEK Launcher";
    /// <summary>String representation of app version.</summary>
    public static readonly string Version;
    /// <summary>Entry point of the application (the first function executed in compiler-generated Main).</summary>
    App()
    {
        Directory.CreateDirectory(AppDataFolder);
        AppDomain.CurrentDomain.UnhandledException += DomainExceptionHandler;
        string cultureCode = CultureInfo.CurrentUICulture.Name; //This will be used later to initialize LocManager
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("en-US"); //Set .NET culture to en-US for exceptions to be in English and type conversions to work properly
        ServicePointManager.DefaultConnectionLimit = 20; //Allow up to 20 simultaneous downloads from the same host, needed for Steam CDN client
        InitializeComponent(); //Load XAML component
        Settings.Load();
        LocManager.Initialize(cultureCode);
        ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(ToolTip), new FrameworkPropertyMetadata(30000));
        FrameworkElement.StyleProperty.OverrideMetadata(typeof(TEKWindow), new FrameworkPropertyMetadata(FindResource(typeof(TEKWindow))));
        try { _pipeServer = new NamedPipeServerStream("TEKLauncher", PipeDirection.Out, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous); }
        catch (IOException)
        {
            //Pipe server creation fails when another instance of the launcher is already running (because maxNumberOfServerInstances in constructor above is 1), so a message should be displayed
            Messages.Show("Error", LocManager.GetString(LocCode.AnotherInstanceRunning));
            Current.Shutdown();
            return;
        }
        new Thread(Steam.TEKInjectorInterop.ListenPipe).Start(_pipeServer);
        Steam.App.Initialize();
        using Process currentProcess = Process.GetCurrentProcess();
        string oldExePath = string.Concat(currentProcess.MainModule!.FileName, ".old");
        if (File.Exists(oldExePath))
            try { File.Delete(oldExePath); }
            catch { }
        if (string.IsNullOrEmpty(Game.Path) || !Directory.Exists(Path.GetPathRoot(Game.Path)))
            new FirstLaunchWindow().Show();
        else
            Initialize();
    }
    /// <summary>Initializes <see cref="Version"/> string.</summary>
    static App()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version!;
        if (version.Revision == 0)
            version = new(version.Major, version.Minor, version.Build); //Trim trailing .0 from string representation
        Version = version.ToString();
    }
    /// <summary>Gets a value that indicates whether the app is shutting down.</summary>
    public static bool ShuttingDown { get; private set; }
    /// <summary>Audio player for Communism mode.</summary>
    public static SoundPlayer? Player { get; set; }
    /// <summary>Handles exceptions caught by the dispatcher.</summary>
    void ExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        File.WriteAllText($@"{AppDataFolder}\DispatcherException.txt", e.Exception.ToString());
        new FatalErrorWindow(e.Exception).ShowDialog();
        e.Handled = true;
        Shutdown();
    }
    /// <summary>Aborts all Steam client tasks and releases all native resources.</summary>
    void ExitHandler(object sender, ExitEventArgs e)
    {
        ShuttingDown = true;
        GameOptionsTab.AbortTask();
        foreach (var window in Current.Windows)
            if (window is UpdaterWindow updaterWindow && updaterWindow.IsSteamTaskActive)
            {
                updaterWindow.AbortTask();
                break;
            }
        UdpClient.Dispose();
        Steam.ServerBrowser.Shutdown();
        Steam.CM.Client.Disconnect();
        Settings.Save();
        _pipeServer?.Dispose();
    }
    /// <summary>Handles exceptions that could not be caught by any other means.</summary>
    static void DomainExceptionHandler(object sender, UnhandledExceptionEventArgs e) => File.WriteAllText($@"{AppDataFolder}\DomainException.txt", e.ExceptionObject.ToString());
    /// <summary>Closes named pipe server before app shutdown for update purposes.</summary>
    public static void ClosePipeServer() => ((App)Current)._pipeServer?.Dispose();
    /// <summary>Loads all remaining data that wasn't loaded in contructor and creates main window.</summary>
    public static void Initialize()
    {
        Game.Initialize();
        Task.Run(Mod.InitializeList);
        Task.Run(Cluster.ReloadLists);
        new MainWindow().Show();
        object? lastLaunchedVersion = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\TEKLauncher")?.GetValue("LastLaunchedVersion");
        if ((string?)lastLaunchedVersion != Version)
        {
            Registry.LocalMachine.CreateSubKey(@"SOFTWARE\TEKLauncher").SetValue("LastLaunchedVersion", Version);
            if (lastLaunchedVersion is not null)
                Task.Delay(300).ContinueWith(t => Current.Dispatcher.Invoke(() => new WhatsNewWindow().Show()));
        }
    }
}