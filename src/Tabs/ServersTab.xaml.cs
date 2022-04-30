using System.Windows.Controls;
using System.Windows.Media;
using TEKLauncher.Controls;
using TEKLauncher.Servers;

namespace TEKLauncher.Tabs;

/// <summary>Tab that displays the list of clusters.</summary>
partial class ServersTab : ContentControl
{
    /// <summary>The number of currently displayed clsuter items.</summary>
    int _numItems;
    /// <summary>Initializes a new instance of Servers tab.</summary>
    public ServersTab()
    {
        InitializeComponent();
        UpdateStatus();
        lock (Cluster.Lan.Servers)
            if (Cluster.Lan.Servers.Count > 0)
                AddItem(new ClusterItem(Cluster.Lan));
        lock (Cluster.Favorites.Servers)
            if (Cluster.Favorites.Servers.Count > 0)
                AddItem(new ClusterItem(Cluster.Favorites));
        lock (Cluster.Unclustered.Servers)
            if (Cluster.Unclustered.Servers.Count > 0)
                AddItem(new ClusterItem(Cluster.Unclustered));
        lock (Cluster.OnlineClusters)
            foreach (var cluster in Cluster.OnlineClusters)
                AddItem(new ClusterItem(cluster));
    }
    /// <summary>Initiates reload of the cluster list.</summary>
    void Refresh(object sender, RoutedEventArgs e) => Task.Run(Cluster.ReloadLists);
    /// <summary>Gets UI item for specified cluster and creates one if it doesn't exist.</summary>
    /// <param name="cluster">Cluster to get the item for.</param>
    internal ClusterItem GetItemForCluster(Cluster cluster)
    {
        foreach (StackPanel stack in Root.Children)
            foreach (ClusterItem item in stack.Children)
                if (item.DataContext == cluster)
                    return item;
        var newItem = new ClusterItem(cluster);
        AddItem(newItem);
        return newItem;
    }
    /// <summary>Adds specified UI cluster item to the list.</summary>
    /// <param name="item">Cluster item to add.</param>
    public void AddItem(ClusterItem item) => ((StackPanel)Root.Children[_numItems++ % 3]).Children.Add(item);
    /// <summary>Clears displayed cluster list.</summary>
    public void Clear()
    {
        _numItems = 0;
        foreach (StackPanel stack in Root.Children)
            stack.Children.Clear();
    }
    /// <summary>Updates displayed status message accordingly to <see cref="Cluster.CurrentStatus"/>.</summary>
    public void UpdateStatus()
    {
        RefreshButton.IsEnabled = Cluster.CurrentStatus != 0;
        switch (Cluster.CurrentStatus)
        {
            case 0:
                Status.Foreground = Brushes.Yellow;
                Status.Text = LocManager.GetString(LocCode.Loading);
                break;
            case 1:
                Status.Foreground = new SolidColorBrush(Color.FromRgb(0x9E, 0x23, 0x13));
                Status.Text = LocManager.GetString(LocCode.ClustersReloadSteamNotRunning);
                break;
            case 2:
                Status.Foreground = new SolidColorBrush(Color.FromRgb(0x9E, 0x23, 0x13));
                Status.Text = LocManager.GetString(LocCode.ClustersReloadFail);
                break;
            case 3:
                Status.Foreground = new SolidColorBrush(Color.FromRgb(0x0A, 0xA6, 0x3E));
                Status.Text = LocManager.GetString(LocCode.ClustersReloadSuccess);
                break;
        }
    }
}