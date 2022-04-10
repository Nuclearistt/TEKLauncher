using System.Diagnostics;
using System.Windows.Controls;
using TEKLauncher.Controls;
using TEKLauncher.Servers;
using TEKLauncher.Windows;

namespace TEKLauncher.Tabs;

/// <summary>Tab that displays cluster information and server list.</summary>
partial class ClusterTab : ContentControl
{
    /// <summary>Initializes a new Cluster tab for specified cluster.</summary>
    /// <param name="cluster">Cluster to create the tab for.</param>
    internal ClusterTab(Cluster cluster)
    {
        InitializeComponent();
        if (cluster.IsSpecialCluster)
            RefreshButton.Visibility = Visibility.Collapsed;
        else
            RefreshButton.IsEnabled = Cluster.CurrentStatus == 3;
        DataContext = cluster;
        NameBlock.Text = cluster.Name;
        if (cluster.Discord is not null)
            JoinDiscordButton.Visibility = Visibility.Visible;
        if (cluster.Hoster is not null)
        {
            HosterBlock.Visibility = Visibility.Visible;
            HosterBlock.Text = string.Format(LocManager.GetString(LocCode.HostedBy), cluster.Hoster);
        }
        if (cluster.Description is not null)
        {
            ServersBlock.HorizontalAlignment = HorizontalAlignment.Left;
            DescriptionBlock.Visibility = Visibility.Visible;
            void AddMultiplierIfSpecified<T>(T? multiplier, LocCode nameCode) where T : struct
            {
                if (multiplier.HasValue)
                {
                    Description.Inlines.Add(LocManager.GetString(nameCode));
                    Description.Inlines.Add($" {multiplier.Value}{(nameCode == LocCode.MaxWildDinoLevel ? null : "x")}\n");
                }
            }
            AddMultiplierIfSpecified(cluster.Description.MaxDinoLvl, LocCode.MaxWildDinoLevel);
            AddMultiplierIfSpecified(cluster.Description.Taming, LocCode.Taming);
            AddMultiplierIfSpecified(cluster.Description.Experience, LocCode.Experience);
            AddMultiplierIfSpecified(cluster.Description.Harvesting, LocCode.Harvesting);
            AddMultiplierIfSpecified(cluster.Description.Breeding, LocCode.Breeding);
            AddMultiplierIfSpecified(cluster.Description.Stacks, LocCode.Stacks);
            if (cluster.Description.Other is not null)
                foreach (string line in cluster.Description.Other)
                    Description.Inlines.Add($"{line}\n");
        }
        foreach (var server in cluster.Servers)
            Servers.Children.Add(new ServerItem(server, cluster));
    }
    /// <summary>Navigates main window back to Servers tab.</summary>
    void Back(object sender, RoutedEventArgs e) => ((MainWindow)Application.Current.MainWindow).Navigate(new ServersTab());
    /// <summary>Follows the cluster's Discord server join link.</summary>
    void JoinDiscord(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo(((Cluster)DataContext).Discord!) { UseShellExecute = true });
    void Refresh(object sender, RoutedEventArgs e)
    {
        RefreshButton.IsEnabled = false;
        var cluster = (Cluster)DataContext;
        cluster.Servers.Clear();
        Servers.Children.Clear();
        Task.Run(delegate
        {
            var servers = Steam.ServerBrowser.GetServers(Steam.ServerBrowser.ServerListType.Online, cluster.Id);
            if (servers is not null)
                Parallel.ForEach(servers, new ParallelOptions { MaxDegreeOfParallelism = 10 }, server =>
                {
                    if (!server.Query())
                        return;
                    cluster.Servers.Add(server);
                    Dispatcher.Invoke(delegate
                    {
                        var tabFrame = ((MainWindow)Application.Current.MainWindow).TabFrame;
                        if (tabFrame.Child is ServersTab serversTab)
                            serversTab.GetItemForCluster(cluster).RefreshCounts();
                        else if (tabFrame.Child == this)
                            AddServer(server);
                    });
                    
                });
            Dispatcher.Invoke(() => RefreshButton.IsEnabled = true);
        });
    }
    /// <summary>Adds a new server item to displayed server list of the tab.</summary>
    /// <param name="server">Server to add an item for.</param>
    internal void AddServer(Server server) => Servers.Children.Add(new ServerItem(server, (Cluster)DataContext));
    /// <summary>Removes server item from displayed server list of the tab.</summary>
    /// <param name="server">Server whose item should be removed.</param>
    internal void RemoveServer(Server server)
    {
        foreach (ServerItem item in Servers.Children)
            if (item.DataContext == server)
            {
                Servers.Children.Remove(item);
                return;
            }
    }
}