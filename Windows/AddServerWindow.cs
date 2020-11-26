using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TEKLauncher.Controls;
using TEKLauncher.Pages;
using TEKLauncher.Servers;
using static System.Net.IPAddress;
using static System.Threading.ThreadPool;
using static System.Windows.DataObject;
using static System.Windows.Media.Brushes;
using static TEKLauncher.App;
using static TEKLauncher.ARK.UserServers;
using static TEKLauncher.Servers.ClustersManager;

namespace TEKLauncher.Windows
{
    public partial class AddServerWindow : Window
    {
        internal AddServerWindow() => InitializeComponent();
        private IPAddress Address;
        private async void Add(object Sender, RoutedEventArgs Args)
        {
            if (Address is null)
                SetStatus("Entered IP is not valid", DarkRed);
            else
            {
                IPField.IsReadOnly = true;
                AddButton.IsEnabled = false;
                SetStatus("Scanning servers on this IP", YellowBrush);
                Server[] Servers = await GetServersForIPAsync(Address);
                IPField.IsReadOnly = false;
                AddButton.IsEnabled = true;
                if (Servers is null)
                    SetStatus("No ARK servers found on this IP", DarkRed);
                else
                {
                    SetStatus($"Successfully found and added {Servers.Length} servers", DarkGreen);
                    if (Instance.CurrentPage is ClusterPage Page && Page.Cluster.Discord is null)
                    {
                        foreach (Server Server in Servers)
                            Page.ServersList.Children.Add(new ServerItem(Server));
                    }
                    UServers.AddRange(Servers);
                    Clusters[5].Servers = UServers.ToArray();
                    QueueUserWorkItem((State) =>
                    {
                        foreach (Server Server in Servers)
                            Server.Refresh();
                    });
                }
            }
        }
        private void Close(object Sender, RoutedEventArgs Args) => Close();
        private void CopyingHandler(object Sender, DataObjectCopyingEventArgs Args)
        {
            if (Args.IsDragDrop)
                Args.CancelCommand();
        }
        private void Drag(object Sender, MouseButtonEventArgs Args) => DragMove();
        private void Minimize(object Sender, RoutedEventArgs Args) => WindowState = WindowState.Minimized;
        private void PastingHandler(object Sender, DataObjectPastingEventArgs Args)
        {
            if (((string)Args.DataObject.GetData(typeof(string))).Any(Symbol => !(char.IsDigit(Symbol) || Symbol == '.')))
                Args.CancelCommand();
        }
        private void SetStatus(string Text, SolidColorBrush Color)
        {
            Status.Foreground = Color;
            Status.Text = Text;
        }
        private void TextBoxLoadedHandler(object Sender, RoutedEventArgs Args)
        {
            AddCopyingHandler(IPField, CopyingHandler);
            AddPastingHandler(IPField, PastingHandler);
        }
        private void TextChangedHandler(object Sender, TextChangedEventArgs Args)
        {
            if (IPField.Text.Count(Symbol => Symbol == '.') == 3 && TryParse(IPField.Text, out Address))
                AddButton.IsEnabled = true;
            else
            {
                Address = null;
                AddButton.IsEnabled = false;
            }
        }
        private void TextInputHandler(object Sender, TextCompositionEventArgs Args) => Args.Handled = !(char.IsDigit(Args.Text[0]) || Args.Text[0] == '.');
    }
}