using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using TEKLauncher.Controls;
using TEKLauncher.SteamInterop.Network.CM.Messages.Bodies;
using static System.Environment;
using static System.Threading.Tasks.Task;
using static System.Windows.DataObject;
using static TEKLauncher.App;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.SteamInterop.Network.SteamClient;

namespace TEKLauncher.Pages
{
    public partial class WorkshopBrowserPage : Page
    {
        public WorkshopBrowserPage() => InitializeComponent();
        ~WorkshopBrowserPage() => Timer.Dispose();
        private bool EditPending;
        private int CurrentPage = 1, EditTimestamp;
        private string CurrentSearch;
        private Timer Timer;
        private void CheckEdits(object State)
        {
            if (EditPending && TickCount - EditTimestamp > 999)
            {
                EditPending = false;
                Dispatcher.Invoke(() => LoadPage(CurrentPage = 1, CurrentSearch = SearchBar.Text));
            }
        }
        private void CopyingHandler(object Sender, DataObjectCopyingEventArgs Args)
        {
            if (Args.IsDragDrop)
                Args.CancelCommand();
        }
        private void GoBack(object Sender, RoutedEventArgs Args) => Instance.MWindow.PageFrame.Content = new ModInstallerPage();
        private void GoNextPage(object Sender, RoutedEventArgs Args) => LoadPage(CurrentPage + 1, CurrentSearch);
        private void GoPrevPage(object Sender, RoutedEventArgs Args) => LoadPage(CurrentPage - 1, CurrentSearch);
        private async void LoadPage(int Page, string Search)
        {
            bool Connected;
            try { Connected = await Run(Connect); }
            catch { Connected = false; }
            if (Connected)
            {
                int TotalCount = 0;
                List<ItemDetails> Result = await Run(() => GetQuery(Page, Search, out TotalCount));
                if (Result is null)
                {
                    ErrorBlock.Text = LocString(LocCode.WBTimeout);
                    ReloadButton.Visibility = ErrorBlock.Visibility = Visibility.Visible;
                }
                else
                {
                    ReloadButton.Visibility = ErrorBlock.Visibility = Visibility.Collapsed;
                    CurrentPage = Page;
                    int PagesCount = TotalCount / 20;
                    if (TotalCount % 20 != 0)
                        PagesCount++;
                    PageBlock.Text = string.Format(LocString(LocCode.WBPage), Page, PagesCount);
                    NextPage.Visibility = Page < PagesCount ? Visibility.Visible : Visibility.Collapsed;
                    PrevPage.Visibility = Page > 1 ? Visibility.Visible : Visibility.Collapsed;
                    ItemsList.Children.Clear();
                    foreach (ItemDetails Details in Result)
                        ItemsList.Children.Add(new WBItem(Details));
                }
            }
            else
            {
                ErrorBlock.Text = LocString(LocCode.ConnectToSteamFailed);
                ReloadButton.Visibility = ErrorBlock.Visibility = Visibility.Visible;
            }
        }
        private void LoadedHandler(object Sender, RoutedEventArgs Args)
        {
            AddCopyingHandler(SearchBar, CopyingHandler);
            LoadPage(1, null);
            Timer = new Timer(CheckEdits, null, 1000, 1000);
        }
        private void ReloadPage(object Sender, RoutedEventArgs Args)
        {
            ReloadButton.Visibility = ErrorBlock.Visibility = Visibility.Collapsed;
            LoadPage(CurrentPage, CurrentSearch);
        }
        private void TextChangedHandler(object Sender, TextChangedEventArgs Args)
        {
            EditPending = true;
            EditTimestamp = TickCount;
        }
    }
}