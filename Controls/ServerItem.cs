using System.Windows;
using System.Windows.Controls;
using TEKLauncher.ARK;
using TEKLauncher.Servers;
using static TEKLauncher.ARK.DLCManager;
using static TEKLauncher.ARK.Game;

namespace TEKLauncher.Controls
{
    public partial class ServerItem : UserControl
    {
        internal ServerItem(Server Server)
        {
            InitializeComponent();
            this.Server = Server;
            ServerName.Text = Server.Name;
            if (Server.Code > MapCode.TheIsland && Server.Code < MapCode.Mod && !GetDLC(Server.Code).IsInstalled)
            {
                DLCInstalled = false;
                JoinButton.IsEnabled = false;
                RefreshWarning();
            }
        }
        private readonly Server Server;
        internal readonly bool DLCInstalled = true;
        private void Join(object Sender, RoutedEventArgs Args) => Launch(Server);
        internal void RefreshWarning()
        {
            if (!DLCInstalled)
            {
                Players.Inlines.Add(new VectorImage { Width = 25D, Height = 25D, Margin = new Thickness(10D, 0D, 0D, 0D), Source = "Warning" });
                Players.Inlines.Add("DLC not installed!");
            }
        }
    }
}