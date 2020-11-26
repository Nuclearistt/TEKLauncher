using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TEKLauncher.ARK;
using TEKLauncher.Controls;
using TEKLauncher.SteamInterop.Network;
using TEKLauncher.Windows;
using static System.IO.File;
using static System.Threading.Tasks.Task;
using static System.Windows.Application;
using static System.Windows.DataObject;
using static System.Windows.Media.Brushes;
using static TEKLauncher.App;
using static TEKLauncher.ARK.ModManager;
using static TEKLauncher.Data.Settings;
using static TEKLauncher.Net.ARKdictedData;
using static TEKLauncher.SteamInterop.SteamworksAPI;
using static TEKLauncher.UI.Message;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.Pages
{
    public partial class ModInstallerPage : Page
    {
        internal ModInstallerPage()
        {
            InitializeComponent();
            Downloader = new ContentDownloader(346110U, FinishHandler, SetStatus, ProgressBar);
        }
        internal ModInstallerPage(string ID) : this() => Loaded += (Sender, Args) => ARKID.Text = ID;
        private ulong ARKModID, SpacewarModID;
        private ModDetails ARKModDetails, SpacewarModDetails;
        private CancellationTokenSource CancellatorA, CancellatorS;
        private Task PreviewTaskA, PreviewTaskS;
        internal bool IsInstalling;
        internal readonly ContentDownloader Downloader;
        private void BrowseWorkshop(object Sender, RoutedEventArgs Args) => Instance.MWindow.PageFrame.Content = new WorkshopBrowserPage();
        private void CopyingHandler(object Sender, DataObjectCopyingEventArgs Args)
        {
            if (Args.IsDragDrop)
                Args.CancelCommand();
        }
        private void DownloadJob()
        {
            Beginning:
            try { Downloader.DownloadMod(ARKModID, SpacewarModID, ref ARKModDetails, ref SpacewarModDetails); }
            catch (Exception Exception)
            {
                Downloader.ReleaseLock();
                if (Exception is AggregateException)
                    Exception = Exception.InnerException;
                if (Exception is ValidatorException)
                {
                    if (Exception.Message == "Download failed" && AutoRetry)
                        goto Beginning;
                    else
                        Dispatcher.Invoke(() =>
                        {
                            SetStatus($"Error: {Exception.Message}", DarkRed);
                            FinishHandler();
                            DeletePath($@"{Game.Path}\ShooterGame\Content\Mods\{SpacewarModID}");
                        });
                }
                else
                {
                    WriteAllText($@"{AppDataFolder}\LastCrash.txt", $"{Exception.Message}\n{Exception.StackTrace}");
                    Dispatcher.Invoke(() =>
                    {
                        new CrashWindow(Exception).ShowDialog();
                        Current.Shutdown();
                    });
                }
                return;
            }
        }
        private async void FinishHandler()
        {
            if (Downloader.FilesUpToDate == 1L)
            {
                SetStatus($"Subscribing {SpacewarModDetails.Name}", YellowBrush);
                bool Subscribed = false;
                if (await TryDeployAsync())
                    if (await SteamAPI.SubscribeModAsync(SpacewarModID))
                        Subscribed = true;
                if (Subscribed)
                    Mods.Last().IsSubscribed = true;
                else
                {
                    Show("Info", "Mod was installed successfully, but couldn't be subscribed automatically. After pressing \"OK\" you will be redirected to Spacewar mod's page to subscribe");
                    Execute($"steam://openurl/https://steamcommunity.com/sharedfiles/filedetails/?id={SpacewarID.Text}");
                }
                SetStatus($"Successfully installed {ARKModDetails.Name}", DarkGreen);
            }
            SpacewarID.IsReadOnly = ARKID.IsReadOnly = false;
            SelectIDButton.IsEnabled = InstallCancelButton.IsEnabled = BackButton.IsEnabled = true;
            ((VectorImage)InstallCancelButton.Template.FindName("Icon", InstallCancelButton)).Source = "Install";
            IsInstalling = false;
        }
        private void GoBack(object Sender, RoutedEventArgs Args)
        {
            Instance.MWindow.ModInstallerMode = false;
            Instance.MWindow.PageFrame.Content = new ModsPage();
        }
        private async void InstallCancel(object Sender, RoutedEventArgs Args)
        {
            if (IsInstalling)
            {
                InstallCancelButton.IsEnabled = false;
                Downloader.Pause();
                SetStatus("Cancelling...", YellowBrush);
            }
            else if (ARKID.Text.Length == 0)
                SetStatus("Enter ARK mod ID", DarkRed);
            else
            {
                ulong ID = ulong.Parse(ARKID.Text);
                if (Workshop.ContainsKey(ID))
                {
                    if (!(Mods.Find(Mod => Mod.ID == Workshop[ID])?.IsInstalled ?? false) && ShowOptions("Info", "A reupload of this mod is available in ARKdicted workshop. Do you want to subscribe it instead? If you press \"Yes\", the mod will be subscribed and downloaded by Steam. If you press \"No\", the launcher will proceed with default installation method"))
                    {
                        ulong ARKDictedID = Workshop[ID];
                        SetStatus("Requesting mod's details", YellowBrush);
                        ModDetails[] Response = await GetModsDetailsAsync(ARKDictedID);
                        ModDetails Details = (Response?.Length ?? 0) == 0 ? default : Response[0];
                        string Name = Details.Status == 1 ? Details.Name : "ARKdicted mod";
                        SetStatus($"Subscribing {Name}", YellowBrush);
                        bool Subscribed = false;
                        if (await TryDeployAsync())
                            if (await SteamAPI.SubscribeModAsync(ARKDictedID))
                                Subscribed = true;
                        if (!Subscribed)
                        {
                            Show("Info", "Failed to subscribe the mod automatically. After pressing \"OK\" you will be redirected to mod's page to subscribe");
                            Execute($"steam://openurl/https://steamcommunity.com/sharedfiles/filedetails/?id={ARKDictedID}");
                        }
                        SetStatus($"Successfully subscribed {Name}", DarkGreen);
                        FinishHandler();
                        return;
                    }
                }
                if (SpacewarID.Text.Length == 0)
                    SetStatus("Enter Spacewar mod ID", DarkRed);
                else if (Mods.Find(Mod => Mod.ID == ulong.Parse(SpacewarID.Text))?.IsInstalled ?? false)
                    SetStatus("Selected Spacewar mod ID is already in use", DarkRed);
                else
                {
                    IsInstalling = true;
                    SpacewarID.IsReadOnly = ARKID.IsReadOnly = true;
                    SelectIDButton.IsEnabled = BackButton.IsEnabled = false;
                    ((VectorImage)InstallCancelButton.Template.FindName("Icon", InstallCancelButton)).Source = "Pause";
                    ARKModID = ID;
                    SpacewarModID = ulong.Parse(SpacewarID.Text);
                    SetStatus("Requesting mods details", YellowBrush);
                    string Message = null;
                    ModDetails[] Details = await GetModsDetailsAsync(ARKModID, SpacewarModID);
                    if ((Details?.Length ?? 0) < 2)
                        Message = "Failed to request mods details, try again";
                    else if (Details[0].Status == 2)
                        Message = "ARK mod with selected ID doesn't exist";
                    else if (Details[0].AppID != 346110U)
                        Message = "Entered ARK mod ID doesn't belong to ARK workshop";
                    else if (Details[1].Status == 2)
                        Message = "Spacewar mod with selected ID doesn't exist";
                    else if (Details[1].AppID != 480U)
                        Message = "Entered Spacewar mod ID doesn't belong to Spacewar workshop";
                    if (Message is null)
                    {
                        ARKModDetails = Details[0];
                        SpacewarModDetails = Details[1];
                        new Thread(DownloadJob).Start();
                    }
                    else
                    {
                        SetStatus(Message, DarkRed);
                        Downloader.FilesUpToDate = 0;
                        FinishHandler();
                    }
                }
            }
        }
        private void LoadPreview(object Args)
        {
            object[] ArgsArray = (object[])Args;
            bool Spacewar = (bool)ArgsArray[0];
            ulong ID = (ulong)ArgsArray[1];
            ModDetails[] Response = GetModsDetails(ID);
            ModDetails Details = Response is null ? default : Response[0];
            Dispatcher.Invoke(() =>
            {
                if (Details.Status == 1)
                {
                    (Spacewar ? PreviewS : PreviewA).Source = new BitmapImage(new Uri(Details.PreviewURL));
                    (Spacewar ? NameS : NameA).Text = Details.Name;
                    if (Spacewar && Details.AppID != 480)
                    {
                        (Spacewar ? ErrorS : ErrorA).Visibility = Visibility.Visible;
                        (Spacewar ? ErrorS : ErrorA).Text = "Not a Spacewar mod";
                    }
                    else if (!Spacewar && Details.AppID != 346110)
                    {
                        (Spacewar ? ErrorS : ErrorA).Visibility = Visibility.Visible;
                        (Spacewar ? ErrorS : ErrorA).Text = "Not an ARK mod";
                    }
                    else
                        (Spacewar ? ErrorS : ErrorA).Visibility = Visibility.Collapsed;
                }
                else
                {
                    (Spacewar ? PreviewS : PreviewA).Source = new BitmapImage();
                    (Spacewar ? NameS : NameA).Text = string.Empty;
                    (Spacewar ? ErrorS : ErrorA).Visibility = Visibility.Visible;
                    (Spacewar ? ErrorS : ErrorA).Text = Details.Status == 0 ? "Failed to load preview" : "Mod with this ID doesn't exist";
                }
            });
        }
        private void PastingHandler(object Sender, DataObjectPastingEventArgs Args)
        {
            if (((string)Args.DataObject.GetData(typeof(string))).Any(Symbol => !char.IsDigit(Symbol)))
                Args.CancelCommand();
        }
        private void SelectID(object Sender, RoutedEventArgs Args)
        {
            IEnumerable<ulong> InstalledIDs = Mods.Select(Mod => Mod.ID);
            foreach (ulong ID in SpacewarIDs)
                if (!InstalledIDs.Contains(ID))
                {
                    SpacewarID.Text = ID.ToString();
                    return;
                }    
            Show("Warning", "All predefined IDs are in use!");
        }
        private void SetStatus(string Text, SolidColorBrush Color)
        {
            Status.Foreground = Color;
            Status.Text = Text;
        }
        private void TextBoxLoadedHandler(object Sender, RoutedEventArgs Args)
        {
            AddCopyingHandler((DependencyObject)Sender, CopyingHandler);
            AddPastingHandler((DependencyObject)Sender, PastingHandler);
        }
        private void TextChangedHandler(object Sender, TextChangedEventArgs Args)
        {
            bool Spacewar = Sender == SpacewarID;
            ref CancellationTokenSource CancellatorObject = ref (Spacewar ? ref CancellatorS : ref CancellatorA);
            ref Task TaskObject = ref (Spacewar ? ref PreviewTaskS : ref PreviewTaskA);
            CancellatorObject?.Cancel();
            string Text = ((TextBox)Sender).Text;
            if (Text.Length == 0)
            {
                (Spacewar ? PreviewS : PreviewA).Source = new BitmapImage();
                (Spacewar ? NameS : NameA).Text = string.Empty;
                (Spacewar ? ErrorS : ErrorA).Visibility = Visibility.Collapsed;
            }
            else
                TaskObject = Factory.StartNew(LoadPreview, new object[] { Spacewar, ulong.Parse(Text) }, (CancellatorObject = new CancellationTokenSource()).Token);
        }
        private void TextInputHandler(object Sender, TextCompositionEventArgs Args) => Args.Handled = !char.IsDigit(Args.Text[0]);
    }
}