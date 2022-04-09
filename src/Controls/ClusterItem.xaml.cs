using System.Windows.Controls;
using System.Windows.Media.Imaging;
using TEKLauncher.Servers;
using TEKLauncher.Tabs;
using TEKLauncher.Windows;

namespace TEKLauncher.Controls;

/// <summary>Represents a cluster in Servers tab GUI.</summary>
partial class ClusterItem : UserControl
{
    /// <summary>Initializes a new item to represent specified cluster.</summary>
    /// <param name="cluster">Cluster that will be represented by the item.</param>
    internal ClusterItem(Cluster cluster)
    {
        InitializeComponent();
        DataContext = cluster;
        NameBlock.Text = cluster.Name;
        if (cluster.IconUrl is null)
            Icon.Height = 32;
        else
            Icon.Source = new BitmapImage(new(cluster.IconUrl));
        RefreshCounts();
    }
    /// <summary>Creates a <see cref="ClusterTab"/> for represented clutser and navigates main window to it.</summary>
    void OpenTab(object sender, RoutedEventArgs e) => ((MainWindow)Application.Current.MainWindow).Navigate(new ClusterTab((Cluster)DataContext));
    /// <summary>Updates player and server counts displayed on the item.</summary>
    public void RefreshCounts()
    {
        var cluster = (Cluster)DataContext;
        int totalPlayers = 0;
        lock (cluster.Servers)
        {
            foreach (var server in cluster.Servers)
                totalPlayers += server.OnlinePlayers;
            Counts.Text = string.Format(LocManager.GetString(LocCode.ClusterCounts), totalPlayers, cluster.Servers.Count);
        }
    }
}