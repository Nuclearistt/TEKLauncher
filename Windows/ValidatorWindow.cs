using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using TEKLauncher.ARK;
using TEKLauncher.Controls;
using TEKLauncher.Pages;
using TEKLauncher.SteamInterop.Network;
using TEKLauncher.UI;
using static System.IO.File;
using static System.Windows.Application;
using static System.Windows.Media.Brushes;
using static TEKLauncher.App;
using static TEKLauncher.ARK.Game;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.UI.Message;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.Windows
{
    public partial class ValidatorWindow : Window
    {
        internal ValidatorWindow(DLC DLC, bool DoValidate)
        {
            this.DoValidate = DoValidate;
            this.DLC = DLC;
            InitializeComponent();
            if (LocCulture == "pt" || LocCulture == "el")
                Button.FontSize = 12D;
            if (LocCulture == "ar")
                foreach (Panel Stack in ValidationBlock.Children)
                    Stack.FlowDirection = FlowDirection.RightToLeft;
            TitleBlock.Text = Title = string.Format(LocString(LocCode.DLCValidator), DLC.Name);
            ProgressBar.ProgressUpdated += ProgressUpdatedHandler;
            LastStatus = DLC.Status;
            DLC.SetStatus(ARK.Status.Updating);
            Downloader = new ContentDownloader(DLC.DepotID, FinishHandler, SetStatus, ProgressBar);
        }
        internal ValidatorWindow(Mod Mod, bool DoValidate)
        {
            this.Mod = Mod;
            InitializeComponent();
            string Name = Mod.OriginID == 0UL ? Mod.Details.Status == 1 ? Mod.Details.Name : Mod.Name : Mod.OriginDetails.Status == 1 ? Mod.OriginDetails.Name : Mod.Name;
            if (Name.Length > 25)
                Name = Name.Substring(0, 25);
            TitleBlock.Text = Title = string.Format(LocString(LocCode.ModValidator), Name);
            ProgressBar.ProgressUpdated += ProgressUpdatedHandler;
            Downloader = new ContentDownloader(Mod.OriginID == 0UL ? 480U : 346110U, FinishHandler, SetStatus, ProgressBar);
            new Thread(UpdateJob).Start(this.DoValidate = DoValidate);
        }
        private bool CloseCancelling, Finished = true, Succeeded;
        internal bool Shutdown = false;
        private readonly bool DoValidate;
        private readonly Status LastStatus;
        private readonly ContentDownloader Downloader;
        private readonly DLC DLC;
        private readonly Mod Mod;
        private void Close(object Sender, RoutedEventArgs Args) => Close();
        private void ClosingHandler(object Sender, CancelEventArgs Args)
        {
            if (Shutdown)
                Downloader.Pause();
            else if (Finished)
            {
                if (Mod is null)
                    DLC.SetStatus(Succeeded ? ARK.Status.Installed : LastStatus);
                else
                {
                    Mod.UpdateAvailable = false;
                    Mod.Status = ARK.Status.Installed;
                    Dispatcher.Invoke(() =>
                    {
                        if (Instance.CurrentPage is ModsPage Page)
                        {
                            ModItem Item = Page.FindItem(Mod);
                            if (!(Item is null))
                                Item.ValidateButton.Visibility = Visibility.Visible;
                        }
                    });
                }
            }
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
            Finished = true;
            if (CloseCancelling)
            {
                if (Mod is null)
                    DLC.SetStatus(LastStatus);
                else
                {
                    Mod.Status = ARK.Status.Installed;
                    Dispatcher.Invoke(() =>
                    {
                        if (Instance.CurrentPage is ModsPage Page)
                        {
                            ModItem Item = Page.FindItem(Mod);
                            if (!(Item is null))
                            {
                                Item.ValidateButton.Visibility = Visibility.Visible;
                                if (Mod.UpdateAvailable)
                                    Item.UpdateButton.Visibility = Visibility.Visible;
                            }
                        }
                    });
                }
                try { Close(); }
                catch { }
            }
            else
            {
                ValidationBlock.Visibility = Visibility.Hidden;
                if (Status.Foreground == DarkGreen && Status.Text != LocString(LocCode.DwPaused) && Status.Text != LocString(LocCode.ValidationPaused))
                {
                    Succeeded = true;
                    TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                    Status.Text += LocString(LocCode.ValidatorCloseWindow);
                    Button.IsEnabled = false;
                }
                else
                {
                    Button.Content = LocString(LocCode.Retry);
                    ((VectorImage)Button.Template.FindName("Icon", Button)).Source = "Update";
                }
            }
        }
        private void LoadedHandler(object Sender, RoutedEventArgs Args)
        {
            if (IsRunning && Mod is null)
            {
                SetStatus(LocString(LocCode.CantUpdateGameRunning), DarkRed);
                Button.Content = LocString(LocCode.Retry);
                ((VectorImage)Button.Template.FindName("Icon", Button)).Source = "Update";
            }
            else if (!IsConnectionAvailable())
            {
                SetStatus(LocString(LocCode.CantUpdateNoInternet), DarkRed);
                Button.Content = LocString(LocCode.Retry);
                ((VectorImage)Button.Template.FindName("Icon", Button)).Source = "Update";
            }
            else
            {
                Finished = false;
                new Thread(UpdateJob).Start(DoValidate);
            }
        }
        private void Minimize(object Sender, RoutedEventArgs Args) => WindowState = WindowState.Minimized;
        private void PauseRetry(object Sender, RoutedEventArgs Args)
        {
            if ((string)Button.Content == LocString(LocCode.Pause))
                Downloader.Pause();
            else if (IsRunning && Mod is null)
                SetStatus(LocString(LocCode.CantUpdateGameRunning), DarkRed);
            else if (!IsConnectionAvailable())
                SetStatus(LocString(LocCode.CantUpdateNoInternet), DarkRed);
            else
            {
                Finished = false;
                new Thread(UpdateJob).Start(DoValidate);
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
        private void UpdateJob(object DoValidate)
        {
            try
            {
                if (Mod is null)
                    Downloader.Update((bool)DoValidate);
                else
                    Downloader.UpdateMod((bool)DoValidate, Mod.OriginID == 0UL ? Mod.ID : Mod.OriginID, Mod.ID);
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