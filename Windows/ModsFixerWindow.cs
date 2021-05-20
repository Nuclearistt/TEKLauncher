using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using TEKLauncher.ARK;
using TEKLauncher.Controls;
using TEKLauncher.SteamInterop;
using TEKLauncher.SteamInterop.Network;
using TEKLauncher.SteamInterop.Network.CM.Messages.Bodies;
using TEKLauncher.UI;
using TEKLauncher.Utils;
using static System.DateTimeOffset;
using static System.Diagnostics.Process;
using static System.IO.File;
using static System.Threading.Tasks.Task;
using static System.Windows.Application;
using static System.Windows.Media.Brushes;
using static TEKLauncher.App;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.SteamInterop.SteamworksAPI;
using static TEKLauncher.SteamInterop.Network.SteamClient;
using static TEKLauncher.UI.Message;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.Windows
{
    public partial class ModsFixerWindow : Window
    {
        internal ModsFixerWindow(ModRecord[] Mods)
        {
            InitializeComponent();
            if (LocCulture == "pt" || LocCulture == "el")
                Button.FontSize = 12D;
            if (LocCulture == "ar")
                foreach (Panel Stack in ValidationBlock.Children)
                    Stack.FlowDirection = FlowDirection.RightToLeft;
            ProgressBar.ProgressUpdated += ProgressUpdatedHandler;
            Downloader = new ContentDownloader(480U, FinishHandler, SetStatus, ProgressBar);
            this.Mods = Mods;
        }
        private bool ACFChanged, CloseCancelling, Finished = true;
        private int ModIndex;
        private VDFStruct ACF;
        internal bool Shutdown = false;
        private readonly ContentDownloader Downloader;
        private readonly ModRecord[] Mods;
        private static string ACFPath
        {
            get
            {
                string WorkshopPath = Steam.WorkshopPath;
                return $@"{WorkshopPath.Substring(0, WorkshopPath.Length - 11)}appworkshop_480.acf";
            }
        }
        private void Close(object Sender, RoutedEventArgs Args) => Close();
        private void ClosingHandler(object Sender, CancelEventArgs Args)
        {
            if (Shutdown)
                Downloader.Pause();
            else if (Finished)
                return;
            else if (ShowOptions("Warning", LocString(LocCode.ValidatorClosePrompt)))
            {
                CloseCancelling = true;
                Downloader.Pause();
                Args.Cancel = true;
                Message.Show("Info", LocString(LocCode.ValidatorWaitingForCancel));
            }
            else
                Args.Cancel = true;
        }
        private void Drag(object Sender, MouseButtonEventArgs Args) => DragMove();
        private void FinishHandler()
        {
            if (CloseCancelling)
            {
                try { Close(); }
                catch { }
            }
            else
            {
                ValidationBlock.Visibility = Visibility.Hidden;
                if (Status.Foreground != DarkGreen || Status.Text == LocString(LocCode.DwPaused) || Status.Text == LocString(LocCode.ValidationPaused))
                {
                    Finished = true;
                    Button.Content = LocString(LocCode.Retry);
                    ((VectorImage)Button.Template.FindName("Icon", Button)).Source = "Update";
                }
            }
        }
        private void LoadedHandler(object Sender, RoutedEventArgs Args)
        {
            if (!IsConnectionAvailable())
            {
                SetStatus(LocString(LocCode.CantUpdateNoInternet), DarkRed);
                Button.Content = LocString(LocCode.Retry);
                ((VectorImage)Button.Template.FindName("Icon", Button)).Source = "Update";
            }
            else
            {
                Finished = false;
                new Thread(VerificationJob).Start();
            }
        }
        private void Minimize(object Sender, RoutedEventArgs Args) => WindowState = WindowState.Minimized;
        private void PauseRetry(object Sender, RoutedEventArgs Args)
        {
            if ((string)Button.Content == LocString(LocCode.Pause))
                Downloader.Pause();
            else if (!IsConnectionAvailable())
                SetStatus(LocString(LocCode.CantUpdateNoInternet), DarkRed);
            else
            {
                Finished = false;
                new Thread(VerificationJob).Start();
                Button.Content = LocString(LocCode.Pause);
                ((VectorImage)Button.Template.FindName("Icon", Button)).Source = "Pause";
            }
        }
        private void ProgressUpdatedHandler()
        {
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            TaskbarItemInfo.ProgressValue = ProgressBar.Progress.Ratio;
            if (Downloader.IsValidating)
            {
                if (ValidationBlock.Visibility == Visibility.Hidden)
                    ValidationBlock.Visibility = Visibility.Visible;
                FilesMissing.Text = Downloader.FilesMissing.ToString();
                FilesOutdated.Text = Downloader.FilesOutdated.ToString();
                FilesUpToDate.Text = Downloader.FilesUpToDate.ToString();
            }
        }
        private void SetStatus(string Text, SolidColorBrush Color)
        {
            Status.Foreground = Color;
            Status.Text = Text;
        }
        private async void VerificationJob()
        {
            try
            {
                string ACFFile = ACFPath;
                bool ACFExists = Exists(ACFFile);
                if (ACF is null && ACFExists)
                    using (StreamReader Reader = new StreamReader(ACFFile))
                        ACF = new VDFStruct(Reader);
                for (; ModIndex < Mods.Length; ModIndex++)
                {
                    Dispatcher.Invoke(() => ModStatus.Text = string.Format(LocString(LocCode.MFVerifying), ModIndex + 1, Mods.Length, Mods[ModIndex].Name));
                    ulong ID = Mods[ModIndex].ID, ManifestID = Downloader.UpdateMod(true, ID, ID);
                    if (Finished)
                        return;
                    if (await TryDeployAsync())
                        await SteamAPI.SubscribeModAsync(ID);
                    if (!ACFExists)
                        continue;
                    VDFStruct ItemsInstalled = ACF["WorkshopItemsInstalled"], ItemDetails = ACF["WorkshopItemDetails"], IIEntry = ItemsInstalled[ID.ToString()], IDEntry = ItemDetails[ID.ToString()];
                    ItemDetails Details = null;
                    void GetItemDetails()
                    {
                        if (!(Details is null))
                            return;
                        if (Connect())
                        {
                            List<ItemDetails> Response = GetDetails(ID);
                            if ((Response?.Count ?? 0) > 0)
                                Details = Response[0];
                        }
                        if (Details is null || Details.Result != 1)
                            throw new ValidatorException(LocString(LocCode.MIRequestFailed));
                    }
                    if (IIEntry is null)
                    {
                        ACFChanged = true;
                        GetItemDetails();
                        IIEntry = new VDFStruct
                        {
                            Key = ID.ToString(),
                            Children = new List<VDFStruct>
                            {
                                new VDFStruct { Key = "size", Value = Details.Size.ToString() },
                                new VDFStruct { Key = "timeupdated", Value = Details.LastUpdated.ToString() },
                                new VDFStruct { Key = "manifest", Value = ManifestID.ToString() }
                            }
                        };
                        ItemsInstalled.Children.Add(IIEntry);
                    }
                    else if (!ulong.TryParse(IIEntry["manifest"].Value, out ulong MID) || MID != ManifestID)
                    {
                        ACFChanged = true;
                        GetItemDetails();
                        IIEntry["size"].Value = Details.Size.ToString();
                        IIEntry["timeupdated"].Value = Details.LastUpdated.ToString();
                        IIEntry["manifest"].Value = ManifestID.ToString();
                    }
                    if (IDEntry is null)
                    {
                        ACFChanged = true;
                        GetItemDetails();
                        IDEntry = new VDFStruct
                        {
                            Key = ID.ToString(),
                            Children = new List<VDFStruct>
                            {
                                new VDFStruct { Key = "manifest", Value = ManifestID.ToString() },
                                new VDFStruct { Key = "timeupdated", Value = Details.LastUpdated.ToString() },
                                new VDFStruct { Key = "timetouched", Value = Now.ToUnixTimeSeconds().ToString() },
                                new VDFStruct { Key = "subscribedby", Value = Steam.ActiveUserID.ToString() },
                            }
                        };
                        ItemDetails.Children.Add(IDEntry);
                    }
                    else if (!ulong.TryParse(IDEntry["manifest"].Value, out ulong MID) || MID != ManifestID)
                    {
                        ACFChanged = true;
                        GetItemDetails();
                        IDEntry["manifest"].Value = ManifestID.ToString();
                        IDEntry["timeupdated"].Value = Details.LastUpdated.ToString();
                        IDEntry["timetouched"].Value = Steam.ActiveUserID.ToString();
                    }
                }
                await Dispatcher.Invoke(async () =>
                {
                    Finished = true;
                    TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                    ModStatus.Text = LocString(LocCode.MFVerificationComplete);
                    if (ACFChanged)
                    {
                        SetStatus(LocString(LocCode.MFApplyingModifications), DarkGreen);
                        if (!Steam.IsRunning)
                            using (StreamWriter Writer = new StreamWriter(ACFPath))
                                ACF.Write(Writer);
                        else if (ShowOptions("Info", LocString(LocCode.MFSteamRestartRequired)))
                        {
                            SetStatus(LocString(LocCode.WaitingForSteamShutdown), YellowBrush);
                            Retract();
                            Start($@"{Steam.Path}\Steam.exe", "-shutdown").WaitForExit();
                            while (Steam.IsRunning)
                                await Delay(1000);
                            using (StreamWriter Writer = new StreamWriter(ACFPath))
                                ACF.Write(Writer);
                            Start($@"{Steam.Path}\Steam.exe");
                        }
                    }
                    SetStatus(LocString(LocCode.MFSuccess), DarkGreen);
                    Button.IsEnabled = false;
                });
            }
            catch (Exception Exception)
            {
                Downloader.ReleaseLock();
                if (Exception is AggregateException)
                    Exception = Exception.InnerException;
                if (Exception is ValidatorException)
                    Dispatcher.Invoke(() =>
                    {
                        SetStatus(string.Format(LocString(LocCode.ValidatorExc), Exception.Message), DarkRed);
                        FinishHandler();
                    });
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
    }
}