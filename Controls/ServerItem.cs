﻿using System.Windows;
using System.Windows.Controls;
using TEKLauncher.ARK;
using TEKLauncher.Servers;
using static TEKLauncher.ARK.DLCManager;
using static TEKLauncher.ARK.Game;
using static TEKLauncher.ARK.UserServers;
using static TEKLauncher.Data.LocalizationManager;

namespace TEKLauncher.Controls
{
    public partial class ServerItem : UserControl
    {
        internal ServerItem(Server Server)
        {
            InitializeComponent();
            this.Server = Server;
            ServerName.Text = Server.Name;
            if (Server.MaxPlayers == 0)
                DeleteButton.Visibility = Visibility.Collapsed;
            if (Server.Code > MapCode.TheIsland && Server.Code < MapCode.Mod && !GetDLC(Server.Code).IsInstalled)
            {
                DLCInstalled = false;
                JoinButton.IsEnabled = false;
                RefreshWarning();
            }
        }
        private readonly Server Server;
        internal readonly bool DLCInstalled = true;
        private void Delete(object Sender, RoutedEventArgs Args)
        {
            UServers.RemoveAll(Item => Item.Key == Server);
            CommitList();
            ((Panel)Parent).Children.Remove(this);
        }
        private void Join(object Sender, RoutedEventArgs Args) => Launch(Server);
        internal void RefreshWarning()
        {
            if (Server.NoIP)
            {
                Players.Inlines.Add(new VectorImage { Width = 25D, Height = 25D, Margin = new Thickness(10D, 0D, 0D, 0D), Source = "Warning" });
                Players.Inlines.Add(LocString(LocCode.IPNotResolved));
            }
            else if (!DLCInstalled)
            {
                Players.Inlines.Add(new VectorImage { Width = 25D, Height = 25D, Margin = new Thickness(10D, 0D, 0D, 0D), Source = "Warning" });
                Players.Inlines.Add(LocString(LocCode.DLCNotInstalled));
            }
        }
    }
}