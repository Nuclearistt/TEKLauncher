using System.Windows;
using System.Windows.Controls;
using TEKLauncher.Pages;
using TEKLauncher.Servers;
using static System.Windows.Media.Brushes;
using static TEKLauncher.App;
using static TEKLauncher.Data.LocalizationManager;

namespace TEKLauncher.Controls
{
    public partial class ClusterItem : UserControl
    {
        internal ClusterItem(Cluster Cluster)
        {
            InitializeComponent();
            this.Cluster = Cluster;
            ClusterName.Text = Cluster.Name;
            if (Cluster.Discord is null)
            {
                if (LocCulture == "pt" || LocCulture == "ar")
                    ClusterName.FontSize = 28D;
            }
            else
            {
                Mode.Foreground = Cluster.IsPvE ? DarkGreen : DarkRed;
                Mode.Text = Cluster.IsPvE ? "PvE" : "PvP";
            }
        }
        internal readonly Cluster Cluster;
        private void OpenPage(object Sender, RoutedEventArgs Args) => Instance.MWindow.PageFrame.Content = new ClusterPage(Cluster);
    }
}