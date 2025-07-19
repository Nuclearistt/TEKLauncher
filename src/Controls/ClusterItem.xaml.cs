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
        if (string.IsNullOrEmpty(cluster.IconUrl))
            Icon.Height = 32;
        else
            Icon.Source = new BitmapImage(new(cluster.IconUrl));
        RefreshNumServers();
    }
    /// <summary>Creates a <see cref="ClusterTab"/> for represented clutser and navigates main window to it.</summary>
    void OpenTab(object sender, RoutedEventArgs e) => ((MainWindow)Application.Current.MainWindow).Navigate(new ClusterTab((Cluster)DataContext));
    /// <summary>Updates the number of servers displayed on the item.</summary>
    public void RefreshNumServers()
    {
        var cluster = (Cluster)DataContext;
        lock (cluster.Servers)
            NumServers.Text = string.Format(LocManager.GetString(LocCode.ClusterNumServers), cluster.Servers.Count);
    }
}