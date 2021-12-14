using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TEKLauncher.ARK;
using TEKLauncher.Controls;
using TEKLauncher.Data;
using TEKLauncher.Net;
using TEKLauncher.Servers;
using TEKLauncher.Windows;
using static System.Array;
using static System.Threading.Tasks.Task;
using static System.Windows.Application;
using static System.Windows.Media.Brushes;
using static TEKLauncher.App;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.Servers.ClustersManager;
using static TEKLauncher.SteamInterop.Steam;
using static TEKLauncher.SteamInterop.SteamworksAPI;
using static TEKLauncher.UI.Message;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.Pages
{
    public partial class ClusterPage : Page
    {
        internal ClusterPage(Cluster Cluster)
        {
            InitializeComponent();
            this.Cluster = Cluster;
            foreach (Server Server in Cluster.Servers)
                ServersList.Children.Add(new ServerItem(Server));
            ServersSynchronizer = new Timer(Callback, null, 0, 1000);
            if (Cluster.Discord is null)
            {
                ClusterName.Text = Cluster.Name;
                AddServersButton.Visibility = Visibility.Visible;
                MenuBorder.Visibility = Visibility.Collapsed;
            }
            else
            {
                string Mode = Cluster.IsPvE ? "PvE" : "PvP";
                ClusterName.Text = string.Format(LocString(LocCode.ClusterName), Cluster.Name, Mode);
                Hoster.Text = string.Format(LocString(LocCode.HostedBy), Cluster.Hoster);
                LoadInfo();
                if (Cluster.Mods is null)
                    ModsRadioButton.Visibility = Visibility.Collapsed;
                else if (Cluster.Mods.Count != 0)
                    LoadMods();
            }
        }
        ~ClusterPage() => ServersSynchronizer?.Dispose();
        private readonly Timer ServersSynchronizer;
        internal readonly Cluster Cluster;
        private void AddServer(object Sender, RoutedEventArgs Args)
        {
            if (!Current.Windows.OfType<AddServerWindow>().Any())
                new AddServerWindow().Show();
        }
        private void Callback(object State) => Dispatcher.Invoke(SyncServers);
        private void FixMods(object Sender, RoutedEventArgs Args)
        {
            if (!IsSpacewarInstalled)
                Show("Error", LocString(LocCode.SpacewarRequired));
            else if (!Current.Windows.OfType<ModsFixerWindow>().Any())
                new ModsFixerWindow(Cluster.Mods[(string)((Button)Sender).Tag]).Show();
        }
        private void GoBack(object Sender, RoutedEventArgs Args) => Instance.MWindow.PageFrame.Content = new ServersPage();
        private void JoinDiscord(object Sender, RoutedEventArgs Args) => Execute(Cluster.Discord);
        private void Refresh(object Sender, RoutedEventArgs Args)
        {
            Cluster.Refresh();
            SyncServers();
        }
        private async void Subscribe(object Sender, RoutedEventArgs Args)
        {
            ContentControl Button = (ContentControl)Sender;
            bool Subscribed = false;
            ulong ID = (ulong)Button.Tag;
            if (await TryDeployAsync())
                if (await SteamAPI.SubscribeModAsync(ID))
                    Subscribed = true;
            if (!Subscribed)
                Dispatcher.Invoke(() => SubscribeManually(ID));
            Button.IsEnabled = false;
            Button.Content = LocString(LocCode.Subscribed);
        }
        private void SubscribeManually(ulong ID)
        {
            Show("Warning", LocString(LocCode.FailedToSub));
            Execute($"{Links.SteamWorkshop}{ID}");
        }
        private void SyncServers()
        {
            for (int Iterator = 0; Iterator < Cluster.Servers.Length; Iterator++)
            {
                Server Server = Cluster.Servers[Iterator];
                ServerItem Item = (ServerItem)ServersList.Children[Iterator];
                Item.Status.Foreground = Server.IsLoaded ? Server.IsOnline ? DarkGreen : DarkRed : Yellow;
                Item.Status.Text = Server.IsLoaded ? Server.IsOnline ? "Online" : "Offline" : LocString(LocCode.Loading);
                int MaxPlayers = Cluster.Discord is null ? Server.MaxPlayers : Cluster.PlayersLimit;
                Item.Players.Text = $"{Server.PlayersOnline}/{MaxPlayers}";
                Item.RefreshWarning();
                Item.JoinButton.IsEnabled = Server.IsLoaded && Server.IsOnline && Item.DLCInstalled;
            }
        }
        private void SwitchTab(object Sender, RoutedEventArgs Args)
        {
            if (IsLoaded)
            {
                int TabIndex = Menu.Children.IndexOf((UIElement)Sender);
                ServersListViewer.Visibility = TabIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
                InfoStack.Visibility = TabIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
                ModsListViewer.Visibility = TabIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        internal void LoadInfo()
        {
            foreach (KeyValuePair<string, string> InfoBlock in Cluster.Info)
            {
                if (InfoBlock.Key != string.Empty)
                    InfoStack.Children.Add(new TextBlock
                    {
                        Margin = new Thickness(0D, 10D, 0D, 0D),
                        Foreground = (SolidColorBrush)FindResource("BrightBrush"),
                        FontSize = 28,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Text = InfoBlock.Key
                    });
                InfoStack.Children.Add(new Border
                {
                    Margin = new Thickness(0D, 5D, 0D, 0D),
                    Padding = new Thickness(50D, 10D, 50D, 10D),
                    Background = (SolidColorBrush)FindResource("DarkestDarkBrush"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    CornerRadius = new CornerRadius(20D),
                    Child = new TextBlock
                    {
                        Foreground = (SolidColorBrush)FindResource("BrightGrayBrush"),
                        FontSize = 20,
                        Text = InfoBlock.Value
                    }
                });
            }
        }
        internal async void LoadMods()
        {
            ulong[] SubscribedMods = await Run(() => TryDeploy() ? SteamAPI.GetSubscribedMods() : new ulong[0]);
            foreach (KeyValuePair<string, ModRecord[]> Mods in Cluster.Mods)
            {
                Grid Header = new Grid { Margin = new Thickness(0D, 10D, 0D, 0D) };
                Header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0D, GridUnitType.Auto) });
                Header.ColumnDefinitions.Add(new ColumnDefinition());
                Header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0D, GridUnitType.Auto) });
                if (Mods.Key != string.Empty)
                {
                    TextBlock Name = new TextBlock
                    {
                        Foreground = (SolidColorBrush)FindResource("BrightBrush"),
                        FontSize = 28,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center,
                        Text = Mods.Key
                    };
                    Grid.SetColumn(Name, 0);
                    Header.Children.Add(Name);
                }
                Button FixModsButton = new Button
                {
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    Tag = Mods.Key,
                    Content = LocString(LocCode.FixMods)
                };
                FixModsButton.Click += FixMods;
                Grid.SetColumn(FixModsButton, 2);
                Header.Children.Add(FixModsButton);
                ModsList.Children.Add(Header);
                foreach (ModRecord Mod in Mods.Value)
                {
                    StackPanel Stack = new StackPanel();
                    Stack.Children.Add(new TextBlock
                    {
                        Foreground = (SolidColorBrush)FindResource("BrightBrush"),
                        FontSize = 24,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Text = $"{Mod.Name} ({Mod.Size})"
                    });
                    Button SubscribeButton = new Button
                    {
                        Template = (ControlTemplate)FindResource("SubscribeButton"),
                        Foreground = (SolidColorBrush)FindResource("BrightGrayBrush"),
                        FontSize = 20,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        IsEnabled = !SubscribedMods.Contains(Mod.ID),
                        Tag = Mod.ID,
                        Content = SubscribedMods.Contains(Mod.ID) ? LocString(LocCode.Subscribed) : LocString(LocCode.Subscribe)
                    };
                    SubscribeButton.Click += Subscribe;
                    Stack.Children.Add(SubscribeButton);
                    ModsList.Children.Add(new Border
                    {
                        Margin = new Thickness(0D, 5D, 0D, 0D),
                        Padding = new Thickness(10D),
                        Background = (SolidColorBrush)FindResource("DarkestDarkBrush"),
                        CornerRadius = new CornerRadius(20D),
                        Child = Stack
                    });
                }
            }
        }
    }
}