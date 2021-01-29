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
using static TEKLauncher.Data.Links;
using static TEKLauncher.Data.LocalizationManager;
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
            if (LocCulture == "ar")
                StatusStack.FlowDirection = SpacewarStack.FlowDirection = ARKStack.FlowDirection = FlowDirection.RightToLeft;
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
                    if (Exception.Message == LocString(LocCode.DownloadFailed) && AutoRetry)
                        goto Beginning;
                    else
                        Dispatcher.Invoke(() =>
                        {
                            SetStatus(string.Format(LocString(LocCode.ValidatorExc), Exception.Message), DarkRed);
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
                SetStatus(string.Format(LocString(LocCode.MISubscribing), SpacewarModDetails.Name), YellowBrush);
                bool Subscribed = false;
                if (await TryDeployAsync())
                    if (await SteamAPI.SubscribeModAsync(SpacewarModID))
                        Subscribed = true;
                if (Subscribed)
                    Mods.Last().IsSubscribed = true;
                else
                {
                    Show("Info", LocString(LocCode.MISubFailed));
                    Execute($"{SteamWorkshop}{SpacewarID.Text}");
                }
                SetStatus(string.Format(LocString(LocCode.MISuccess), ARKModDetails.Name), DarkGreen);
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
                SetStatus(LocString(LocCode.Cancelling), YellowBrush);
            }
            else if (ARKID.Text.Length == 0)
                SetStatus(LocString(LocCode.MIEnterARKID), DarkRed);
            else
            {
                ulong ID = ulong.Parse(ARKID.Text);
                if (Workshop.ContainsKey(ID))
                {
                    if (!(Mods.Find(Mod => Mod.ID == Workshop[ID])?.IsInstalled ?? false) && ShowOptions("Info", LocString(LocCode.MIARKdictedPrompt)))
                    {
                        ulong ARKDictedID = Workshop[ID];
                        SetStatus(LocString(LocCode.MIRequestingDetails), YellowBrush);
                        ModDetails[] Response = await GetModsDetailsAsync(ARKDictedID);
                        ModDetails Details = (Response?.Length ?? 0) == 0 ? default : Response[0];
                        string Name = Details.Status == 1 ? Details.Name : "ARKdicted mod";
                        SetStatus(string.Format(LocString(LocCode.MISubscribing), Name), YellowBrush);
                        bool Subscribed = false;
                        if (await TryDeployAsync())
                            if (await SteamAPI.SubscribeModAsync(ARKDictedID))
                                Subscribed = true;
                        if (!Subscribed)
                        {
                            Show("Info", LocString(LocCode.FailedToSub));
                            Execute($"{SteamWorkshop}{ARKDictedID}");
                        }
                        SetStatus(string.Format(LocString(LocCode.MISubSuccess), Name), DarkGreen);
                        FinishHandler();
                        return;
                    }
                }
                if (SpacewarID.Text.Length == 0)
                    SetStatus(LocString(LocCode.MIEnterSpacewarID), DarkRed);
                else if (Mods.Find(Mod => Mod.ID == ulong.Parse(SpacewarID.Text))?.IsInstalled ?? false)
                    SetStatus(LocString(LocCode.MIIDInUse), DarkRed);
                else
                {
                    IsInstalling = true;
                    SpacewarID.IsReadOnly = ARKID.IsReadOnly = true;
                    SelectIDButton.IsEnabled = BackButton.IsEnabled = false;
                    ((VectorImage)InstallCancelButton.Template.FindName("Icon", InstallCancelButton)).Source = "Pause";
                    ARKModID = ID;
                    SpacewarModID = ulong.Parse(SpacewarID.Text);
                    SetStatus(LocString(LocCode.MIRequestingDetails), YellowBrush);
                    string Message = null;
                    ModDetails[] Details = await GetModsDetailsAsync(ARKModID, SpacewarModID);
                    if ((Details?.Length ?? 0) < 2)
                        Message = LocString(LocCode.MIRequestFailed);
                    else if (Details[0].Status == 2)
                        Message = LocString(LocCode.MINoARKMod);
                    else if (Details[0].AppID != 346110U)
                        Message = LocString(LocCode.MINotAnARKMod);
                    else if (Details[1].Status == 2)
                        Message = LocString(LocCode.MINoSpacewarMod);
                    else if (Details[1].AppID != 480U)
                        Message = LocString(LocCode.MINotASpacewarMod);
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
                    (Spacewar ? PreviewS : PreviewA).Source = string.IsNullOrEmpty(Details.PreviewURL) ? new BitmapImage() : new BitmapImage(new Uri(Details.PreviewURL));
                    (Spacewar ? NameS : NameA).Text = Details.Name;
                    if (Spacewar && Details.AppID != 480)
                    {
                        (Spacewar ? ErrorS : ErrorA).Visibility = Visibility.Visible;
                        (Spacewar ? ErrorS : ErrorA).Text = LocString(LocCode.MIPvNotASpacewarMod);
                    }
                    else if (!Spacewar && Details.AppID != 346110)
                    {
                        (Spacewar ? ErrorS : ErrorA).Visibility = Visibility.Visible;
                        (Spacewar ? ErrorS : ErrorA).Text = LocString(LocCode.MIPvNotAnARKMod);
                    }
                    else
                        (Spacewar ? ErrorS : ErrorA).Visibility = Visibility.Collapsed;
                }
                else
                {
                    (Spacewar ? PreviewS : PreviewA).Source = new BitmapImage();
                    (Spacewar ? NameS : NameA).Text = string.Empty;
                    (Spacewar ? ErrorS : ErrorA).Visibility = Visibility.Visible;
                    (Spacewar ? ErrorS : ErrorA).Text = Details.Status == 0 ? LocString(LocCode.MIPvFailed) : LocString(LocCode.MIPvNoMod);
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
            Show("Warning", LocString(LocCode.MIAllIDsUsed));
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