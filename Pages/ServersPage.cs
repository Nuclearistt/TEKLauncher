using System.Linq;
using System.Threading;
using System.Windows.Controls;
using TEKLauncher.Controls;
using TEKLauncher.Servers;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.Servers.ClustersManager;

namespace TEKLauncher.Pages
{
    public partial class ServersPage : Page
    {
        public ServersPage()
        {
            InitializeComponent();
            for (int Iterator = 0; Iterator < Clusters.Length; Iterator++)
            {
                if (Iterator / 3 == Clusters.Length / 3)
                    switch(Clusters.Length % 3)
                    {
                        case 1: AddItem(1, Iterator); break;
                        case 2: AddItem(Iterator % 3 == 0 ? 0 : 2, Iterator); break;
                    }
                else
                    AddItem(Iterator % 3, Iterator);
            }
            CountsSynchronizer = new Timer(Callback, null, 0, 1000);
        }
        ~ServersPage() => CountsSynchronizer.Dispose();
        private readonly Timer CountsSynchronizer;
        private void AddItem(int StackIndex, int ClusterIndex) => ((Panel)RootGrid.Children[StackIndex]).Children.Add(new ClusterItem(Clusters[ClusterIndex]));
        private void Callback(object State) => Dispatcher.Invoke(SyncCounts);
        private void SyncCounts()
        {
            for (int Iterator = 0; Iterator < Clusters.Length; Iterator++)
            {
                Cluster Cluster = Clusters[Iterator];
                ClusterItem Item = FindItem(Cluster);
                Item.Count.Text = string.Format(LocString(LocCode.ClusterServers), Cluster.Servers.Length);
                if (Cluster.Servers.Any(Server => Server.IsLoaded))
                    Item.Count.Text += string.Format(LocString(LocCode.ClusterPlayers), Cluster.Servers.Sum(Server => Server.PlayersOnline));
            }
        }
        private ClusterItem FindItem(Cluster Cluster)
        {
            foreach (Panel Stack in RootGrid.Children)
                foreach (ClusterItem Item in Stack.Children)
                    if (Item.Cluster == Cluster)
                        return Item;
            return null;
        }
    }
}