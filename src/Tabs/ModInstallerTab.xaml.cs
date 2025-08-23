using System.Threading;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TEKLauncher.Controls;
using TEKLauncher.Windows;

namespace TEKLauncher.Tabs;

/// <summary>Tab that provides mod installer.</summary>
partial class ModInstallerTab : ContentControl
{
    /// <summary>Indicates whether mod ID written in the textbox has been changed during past 200 ms.</summary>
    bool _idChangePending;
    /// <summary>Indicates whether search query text has been changed during past 200 ms.</summary>
    bool _searchChangePending;
    /// <summary>Current page of Steam workshop browser.</summary>
    uint _currentPage = 1;
    /// <summary>Timestamp of the last time the mod ID was changed.</summary>
    long _lastIdChangeTime;
    /// <summary>Timestamp of the last time the search query text was changed.</summary>
    long _lastSearchChangeTime;
    /// <summary>Current search query.</summary>
    string? _currentSearch;
    /// <summary>Details of the current selected mod.</summary>
    Mod.ModDetails _modDetails;
    /// <summary>Timer that processes mod ID and search query changes.</summary>
    readonly Timer _timer;
    /// <summary>Initializes a new instance of Mod installer tab.</summary>
    public ModInstallerTab()
    {
        InitializeComponent();
        _timer = new(TimerCallback);
    }
    ~ModInstallerTab() => _timer.Dispose();
    /// <summary>Navigates main window back to Mods tab.</summary>
    void Back(object sender, RoutedEventArgs e) => ((MainWindow)Application.Current.MainWindow).Navigate(new ModsTab());
    /// <summary>Creates an <see cref="UpdaterWindow"/> for installing selected mod.</summary>
    void Install(object sender, RoutedEventArgs e)
    {
        foreach (var window in Application.Current.Windows)
            if (window is UpdaterWindow updaterWindow && updaterWindow.Item is Mod.ModDetails details && details.Id == _modDetails.Id)
                return;
		if (TEKSteamClient.Ctx == null)
		{
			Messages.Show("Error", "tek-steamclient library is not loaded");
			return;
		}
		new UpdaterWindow(in _modDetails, false).Show();
    }
    /// <summary>Sets up copying/pasting handlers, starts the timer and loads initial workshop page.</summary>
    void LoadedHandler(object sender, RoutedEventArgs e)
    {
        DataObject.AddCopyingHandler(ModIdBox, (sender, e) =>
        {
            if (e.IsDragDrop)
                e.CancelCommand();
        });
        DataObject.AddCopyingHandler(SearchBar, (sender, e) =>
        {
            if (e.IsDragDrop)
                e.CancelCommand();
        });
        DataObject.AddPastingHandler(ModIdBox, (sender, e) =>
        {
            foreach (char ch in (string)e.DataObject.GetData(typeof(string)))
                if (!char.IsDigit(ch))
                {
                    e.CancelCommand();
                    break;
                }
        });
        _timer.Change(1000, 1000);
        Task.Run(() => LoadWorkshopPage(1));
    }
    /// <summary>Initiates loading next workshop page.</summary>
    void LoadNextPage(object sender, RoutedEventArgs e) => Task.Run(() => LoadWorkshopPage(_currentPage + 1));
    /// <summary>Initiates loading previous workshop page.</summary>
    void LoadPrevPage(object sender, RoutedEventArgs e) => Task.Run(() => LoadWorkshopPage(_currentPage - 1));
    /// <summary>Loads specified workshop page.</summary>
    /// <param name="page">1-based index of the page.</param>
    void LoadWorkshopPage(uint page)
    {
        var details = Steam.CM.Client.QueryMods(page, _currentSearch, out uint total);
        if (details.Length == 0)
        {
            Dispatcher.Invoke(delegate
            {
                SearchError.Text = LocManager.GetString(LocCode.FailedToLoadWorkshopModList);
                ReloadButton.Visibility = SearchError.Visibility = Visibility.Visible;
            });
            return;
        }
        _currentPage = page;
        uint numPages = total / 20;
        if (total % 20 != 0)
            numPages++;
        Dispatcher.Invoke(delegate
        {
            PageBlock.Text = string.Format(LocManager.GetString(LocCode.WorkshopPage), page, numPages);
            NextPage.Visibility = page < numPages ? Visibility.Visible : Visibility.Collapsed;
            PrevPage.Visibility = page > 1 ? Visibility.Visible : Visibility.Collapsed;
            Items.Children.Clear();
            foreach (var item in details)
                Items.Children.Add(new WorkshopItem(in item, id => ModIdBox.Text = id.ToString()));
        });
    }
    /// <summary>Initiates reloading current workshop page.</summary>
    void ReloadWorkshopPage(object sender, RoutedEventArgs e)
    {
        ReloadButton.Visibility = SearchError.Visibility = Visibility.Collapsed;
        Task.Run(() => LoadWorkshopPage(_currentPage));
    }
    /// <summary>Sets pending change status for sender textbox.</summary>
    void TextChangedHandler(object sender, TextChangedEventArgs e)
    {
        if (sender == ModIdBox)
        {
            _lastIdChangeTime = Environment.TickCount64;
            _idChangePending = true;
        }
        else
        {
            _lastSearchChangeTime = Environment.TickCount64;
            _searchChangePending = true;
        }
    }
    /// <summary>Ensures that only numbers are typed into the textbox.</summary>
    void TextInputHandler(object sender, TextCompositionEventArgs e) => e.Handled = !char.IsDigit(e.Text[0]);
    /// <summary>Checks if there are any pending mod ID or search query changes and loads new details if there are.</summary>
    void TimerCallback(object? state)
    {
        long currentTime = Environment.TickCount64;
        if (_idChangePending && currentTime - _lastIdChangeTime >= 200)
        {
            _idChangePending = false;
            _modDetails = default;
            if (ulong.TryParse(Dispatcher.Invoke(() => ModIdBox.Text), out ulong id))
            {
                var response = Steam.CM.Client.GetModDetails(id);
                var details = response.Length == 0 ? default : response[0];
                Dispatcher.Invoke(delegate
                {
                    if (details.Status == 1)
                    {
                        Preview.Source = string.IsNullOrEmpty(details.PreviewUrl) ? new BitmapImage() : new BitmapImage(new(details.PreviewUrl));
                        if (details.AppId == 346110)
                        {
                            PreviewText.Foreground = new SolidColorBrush(Color.FromRgb(0x9F, 0xD6, 0xD2));
                            PreviewText.Text = details.Name;
                            InstallButton.IsEnabled = true;
                            _modDetails = details;
                        }
                        else
                        {
                            PreviewText.Foreground = new SolidColorBrush(Color.FromRgb(0x9E, 0x23, 0x13));
                            PreviewText.Text = LocManager.GetString(LocCode.NotAnARKMod);
                            InstallButton.IsEnabled = false;
                        }
                    }
                    else
                    {
                        Preview.Source = new BitmapImage();
                        PreviewText.Foreground = new SolidColorBrush(Color.FromRgb(0x9E, 0x23, 0x13));
                        PreviewText.Text = LocManager.GetString(details.Status == 0 ? LocCode.FailedToLoadPreview : LocCode.ModWithThisIdDoesntExist);
                        InstallButton.IsEnabled = false;
                    }
                });
            }
            else
                Dispatcher.Invoke(delegate
                {
                    Preview.Source = new BitmapImage();
                    PreviewText.Text = string.Empty;
                    InstallButton.IsEnabled = false;
                });
        }
        if (_searchChangePending && currentTime - _lastSearchChangeTime >= 200)
        {
            _searchChangePending = false;
            _currentSearch = Dispatcher.Invoke(() => SearchBar.Text);
            _currentPage = 1;
            LoadWorkshopPage(1);
        }
    }
}