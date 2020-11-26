using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TEKLauncher.Controls;
using TEKLauncher.Servers;
using TEKLauncher.Windows;
using static System.Windows.Application;
using static System.Windows.Media.Brushes;
using static TEKLauncher.App;
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
            if (Cluster.Name == "Your servers")
            {
                ClusterName.Text = "Your servers";
                MenuBorder.Visibility = Visibility.Collapsed;
            }
            else
            {
                string Mode = Cluster.IsPvE ? "PvE" : "PvP";
                ClusterName.Text = $"{Cluster.Name} {Mode} cluster";
                Hoster.Text = $"Hosted by {Cluster.Hoster}";
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
                if (Cluster.Mods is null)
                    ModsRadioButton.Visibility = Visibility.Collapsed;
                else
                {
                    ulong[] SubscribedMods = new ulong[0];
                    if (TryDeploy())
                        SubscribedMods = SteamAPI.GetSubscribedMods();
                    foreach (KeyValuePair<string, Dictionary<ulong, string>> Mods in Cluster.Mods)
                    {
                        if (Mods.Key != string.Empty)
                            ModsList.Children.Add(new TextBlock
                            {
                                Margin = new Thickness(0D, 10D, 0D, 0D),
                                Foreground = (SolidColorBrush)FindResource("BrightBrush"),
                                FontSize = 28,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Text = Mods.Key
                            });
                        foreach (KeyValuePair<ulong, string> Mod in Mods.Value)
                        {
                            StackPanel Stack = new StackPanel();
                            Stack.Children.Add(new TextBlock
                            {
                                Foreground = (SolidColorBrush)FindResource("BrightBrush"),
                                FontSize = 24,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Text = Mod.Value
                            });
                            Button SubscribeButton = new Button
                            {
                                Template = (ControlTemplate)FindResource("SubscribeButton"),
                                Foreground = (SolidColorBrush)FindResource("BrightGrayBrush"),
                                FontSize = 20,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                IsEnabled = !SubscribedMods.Contains(Mod.Key),
                                Tag = Mod.Key,
                                Content = SubscribedMods.Contains(Mod.Key) ? "Subscribed" : "Subscribe"
                            };
                            SubscribeButton.Click += Subscribe;
                            Stack.Children.Add(SubscribeButton);
                            ModsList.Children.Add(new Border
                            {
                                Margin = new Thickness(60D, 5D, 60D, 0D),
                                Padding = new Thickness(0D, 10D, 0D, 10D),
                                Background = (SolidColorBrush)FindResource("DarkestDarkBrush"),
                                CornerRadius = new CornerRadius(20D),
                                Child = Stack
                            });
                        }
                    }
                }
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
            Button.Content = "Subscribed";
        }
        private void SubscribeManually(ulong ID)
        {
            Show("Warning", "Failed to subscribe the mod automatically, after pressing \"OK\" you'll be redirected to mod's page to subscribe it manually");
            Execute($"steam://openurl/https://steamcommunity.com/sharedfiles/filedetails/?id={ID}");
        }
        private void SyncServers()
        {
            for (int Iterator = 0; Iterator < Cluster.Servers.Length; Iterator++)
            {
                Server Server = Cluster.Servers[Iterator];
                ServerItem Item = (ServerItem)ServersList.Children[Iterator];
                Item.Status.Foreground = Server.IsLoaded ? Server.IsOnline ? DarkGreen : DarkRed : Yellow;
                Item.Status.Text = Server.IsLoaded ? Server.IsOnline ? "Online" : "Offline" : "Loading...";
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
    }
}