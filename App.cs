using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using TEKLauncher.ARK;
using TEKLauncher.Data;
using TEKLauncher.SteamInterop;
using TEKLauncher.Windows;
using static System.Environment;
using static System.IntPtr;
using static System.Globalization.CultureInfo;
using static System.IO.Directory;
using static System.Net.ServicePointManager;
using static System.Net.WebRequest;
using static System.Text.Encoding;
using static System.Threading.Thread;
using static System.Threading.ThreadPool;
using static System.Windows.FrameworkElement;
using static Microsoft.Win32.Registry;
using static TEKLauncher.ARK.ModManager;
using static TEKLauncher.ARK.UserServers;
using static TEKLauncher.Data.Links;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.Net.ARKdictedData;
using static TEKLauncher.SteamInterop.SteamworksAPI;
using static TEKLauncher.SteamInterop.Network.Logger;
using static TEKLauncher.SteamInterop.Network.SteamClient;
using static TEKLauncher.Utils.UtilFunctions;
using static TEKLauncher.Utils.WinAPI;

namespace TEKLauncher
{
    public partial class App : Application
    {
        internal const string Version = "7.2.61.0";
        private App()
        {
            string CultureCode = CurrentUICulture.Name;
            CurrentThread.CurrentCulture = CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            if (OSVersion.Version.Minor == 1)
            {
                Expect100Continue = true;
                SecurityProtocol = SecurityProtocolType.Tls12;
            }
            using (NamedPipeClientStream PipeClient = new NamedPipeClientStream(".", "TEKLauncher", PipeDirection.Out))
                try { PipeClient.Connect(250); PipeClient.Close(); Current.Shutdown(); }
                catch (TimeoutException) { }
            PipeServer = new NamedPipeServerStream("TEKLauncher", PipeDirection.In);
            SetDLLDirectory(AppDataFolder);
            if (OpenProcessToken(GetCurrentProcess(), 32, out IntPtr ProcessToken))
            {
                try
                {
                    TokenPrivileges Privileges = new TokenPrivileges() { PrivilegeCount = 1, Privileges = new LUIDWithAttributes[1] };
                    LookupPrivilegeValue(null, "SeIncreaseQuotaPrivilege", ref Privileges.Privileges[0].LUID);
                    Privileges.Privileges[0].Attributes = 2;
                    AdjustTokenPrivileges(ProcessToken, false, ref Privileges, 0, Zero, Zero);
                }
                finally { CloseHandle(ProcessToken); }
            }
            InitializeComponent();
            Settings.Initialize();
            LoadLocalization(CultureCode);
            YellowBrush = (SolidColorBrush)FindResource("YellowBrush");
            StyleProperty.OverrideMetadata(typeof(Page), new FrameworkPropertyMetadata { DefaultValue = FindResource(typeof(Page)) });
            StyleProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata { DefaultValue = FindResource(typeof(Window)) });
            if (LocCulture == "ar")
                StyleProperty.OverrideMetadata(typeof(TextBlock), new FrameworkPropertyMetadata { DefaultValue = FindResource("RTLTB") });
            QueueUserWorkItem(CheckTrackerInfo);
            using (Process CurrentProcess = Process.GetCurrentProcess())
            {
                string OldExecutable = $"{CurrentProcess.MainModule.FileName}.old";
                if (FileExists(OldExecutable))
                    try { File.Delete(OldExecutable); }
                    catch { }
            }
            foreach (string Folder in new[] { $@"{CurrentDirectory}\ARK", $@"{CurrentDirectory}\ARKSurvivalEvolved", $@"{CurrentDirectory}\ArkGameData" })
                if (Exists(Folder))
                    Settings.ARKPath = Folder;
            if (Settings.KeyExists("ARKPath"))
                Initialize();
            else
                new StartupWindow().Show();
        }
        internal MainWindow MWindow;
        internal readonly NamedPipeServerStream PipeServer;
        internal static bool InstallMode = false;
        internal static string DownloadsDirectory, ManifestsDirectory;
        internal static SolidColorBrush YellowBrush;
        internal static readonly string AppDataFolder = $@"{GetFolderPath(SpecialFolder.ApplicationData)}\TEK Launcher";
        internal object CurrentPage => MWindow?.PageFrame?.Content;
        internal static App Instance => (App)Current;
        private void CheckTrackerInfo(object State)
        {
            if (LocalMachine.OpenSubKey(@"SOFTWARE\TEKLauncher")?.GetValue("Tracked") is null)
            {
                HttpWebRequest Request = CreateHttp(TrackerWebhook);
                Request.ContentType = "application/json";
                Request.Method = "POST";
                Request.Timeout = 4000;
                byte[] Content = UTF8.GetBytes(@"{""content"":""New TEK Launcher user detected!""}");
                try
                {
                    using (Stream RequestStream = Request.GetRequestStream())
                        RequestStream.Write(Content, 0, Content.Length);
                    Request.GetResponse().Dispose();
                    LocalMachine.CreateSubKey(@"SOFTWARE\TEKLauncher").SetValue("Tracked", 1);
                }
                catch { }
            }
        }
        private void ExceptionHandler(object Sender, DispatcherUnhandledExceptionEventArgs Args)
        {
            Exception Exception = Args.Exception;
            if (Exception is AggregateException)
                Exception = Exception.InnerException;
            File.WriteAllText($@"{AppDataFolder}\LastCrash.txt", $"{Exception.Message}\r\n{Exception.StackTrace}");
            new CrashWindow(Exception).ShowDialog();
            Args.Handled = true;
            Shutdown();
        }
        private void ExitHandler(object Sender, ExitEventArgs Args)
        {
            MWindow?.SettingsPage?.SteamDownloader?.Pause();
            MWindow?.ModInstallerPage?.Downloader?.Pause();
            foreach (ValidatorWindow DLCWindow in Windows.OfType<ValidatorWindow>())
            {
                DLCWindow.Shutdown = true;
                DLCWindow.Close();
            }
            PipeServer?.Close();
            Disconnect();
            Close();
            Retract();
            SaveList();
            Settings.Save();
            Environment.Exit(0);
        }
        private void ShowChangelog()
        {
            object LastLaunchedVersion = LocalMachine.OpenSubKey(@"SOFTWARE\TEKLauncher")?.GetValue("LastLaunchedVersion");
            if ((string)LastLaunchedVersion != Version)
            {
                LocalMachine.CreateSubKey(@"SOFTWARE\TEKLauncher").SetValue("LastLaunchedVersion", Version);
                if (!(LastLaunchedVersion is null))
                    new ChangelogWindow().Show();
            }
        }
        internal void Initialize()
        {
            Steam.Initialize();
            Game.Initialize();
            LaunchParameters.Initialize();
            LoadList();
            QueueUserWorkItem(FetchSpacewarIDs);
            QueueUserWorkItem(LoadModsDetails);
            QueueUserWorkItem(LoadServers);
            QueueUserWorkItem(LoadWorkshop);
            CreateDirectory(DownloadsDirectory = $@"{Game.Path}\Downloads").Attributes = FileAttributes.Directory | FileAttributes.Hidden;
            CreateDirectory(ManifestsDirectory = $@"{Game.Path}\Manifests").Attributes = FileAttributes.Directory | FileAttributes.Hidden;
            (MWindow = new MainWindow()).Show();
            ShowChangelog();
            ShutdownMode = ShutdownMode.OnLastWindowClose;
        }
    }
}