using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TEKLauncher.Controls;
using TEKLauncher.Pages;
using TEKLauncher.Servers;
using static System.Convert;
using static System.Net.Dns;
using static System.Net.IPAddress;
using static System.Threading.ThreadPool;
using static System.Windows.DataObject;
using static System.Windows.Media.Brushes;
using static TEKLauncher.App;
using static TEKLauncher.ARK.UserServers;
using static TEKLauncher.Data.LocalizationManager;

namespace TEKLauncher.Windows
{
    public partial class AddServerWindow : Window
    {
        internal AddServerWindow()
        {
            InitializeComponent();
            if (LocCulture == "es")
                Status.FontSize = 20D;
            else if (LocCulture == "ar")
                StatusStack.FlowDirection = FlowDirection.RightToLeft;
        }
        private static readonly Regex HostnameRegex = new Regex("^(([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\\-]*[a-zA-Z0-9])\\.)*([A-Za-z0-9]|[A-Za-z0-9][A-Za-z0-9\\-]*[A-Za-z0-9])$"), IPRegex = new Regex("^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$");
        private async void Add(object Sender, RoutedEventArgs Args)
        {
            string Address = AddressField.Text, Host;
            int Port = -1;
            if (Address.Count(Symbol => Symbol == ':') == 0)
                Host = Address;
            else
            {
                int ColonIndex = Address.IndexOf(':');
                Host = Address.Substring(0, ColonIndex);
                Port = int.Parse(Address.Substring(++ColonIndex));
            }
            bool IsDomain = false;
            IPAddress IP = null;
            if (!(IPRegex.IsMatch(Host) && TryParse(Host, out IP)))
            {
                IsDomain = true;
                SetStatus(LocString(LocCode.ResolvingIP), YellowBrush);
                try { IP = (await GetHostAddressesAsync(Host))[0]; }
                catch { }
            }
            if (IP is null)
                SetStatus(LocString(LocCode.NoIPFound), DarkRed);
            else
            {
                AddressField.IsReadOnly = true;
                AddButton.IsEnabled = false;
                if (Port == -1)
                {
                    SetStatus(LocString(LocCode.ScanningServers), YellowBrush);
                    Server[] Servers = await GetServersForIPAsync(IP);
                    if (Servers is null)
                        SetStatus(LocString(LocCode.NoServersFound), DarkRed);
                    else
                    {
                        SetStatus(string.Format(LocString(LocCode.AddServersSuccess), Servers.Length), DarkGreen);
                        if (Instance.CurrentPage is ClusterPage Page && Page.Cluster.Discord is null)
                        {
                            foreach (Server Server in Servers)
                                Page.ServersList.Children.Add(new ServerItem(Server));
                        }
                        foreach (Server Server in Servers)
                            UServers.Add(new KeyValuePair<Server, string>(Server, IsDomain ? Host : null));
                        CommitList();
                        QueueUserWorkItem((State) =>
                        {
                            foreach (Server Server in Servers)
                                Server.Refresh();
                        });
                    }
                }
                else
                {
                    SetStatus(LocString(LocCode.RequestingSrvInfo), YellowBrush);
                    Server Server = await ResolveServerAsync(IP, Port);
                    if (Server is null)
                        SetStatus(LocString(LocCode.SrvDidnRespond), DarkRed);
                    else if ((int)Server.Code == -1)
                        SetStatus(LocString(LocCode.SrvNotSpacewar), DarkRed);
                    else
                    {
                        SetStatus(LocString(LocCode.AddServerSuccess), DarkGreen);
                        if (Instance.CurrentPage is ClusterPage Page && Page.Cluster.Discord is null)
                            Page.ServersList.Children.Add(new ServerItem(Server));
                        UServers.Add(new KeyValuePair<Server, string>(Server, IsDomain ? Host : null));
                        CommitList();
                        QueueUserWorkItem((State) => Server.Refresh());
                    }
                }
                AddressField.IsReadOnly = false;
                AddButton.IsEnabled = true;
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
            if (((string)Args.DataObject.GetData(typeof(string))).Any(InvalidChar))
                Args.CancelCommand();
        }
        private void SetStatus(string Text, SolidColorBrush Color)
        {
            Status.Foreground = Color;
            Status.Text = Text;
        }
        private void TextBoxLoadedHandler(object Sender, RoutedEventArgs Args)
        {
            AddCopyingHandler(AddressField, CopyingHandler);
            AddPastingHandler(AddressField, PastingHandler);
        }
        private void TextChangedHandler(object Sender, TextChangedEventArgs Args)
        {
            string NewText = AddressField.Text;
            int ColonCount = NewText.Count(Symbol => Symbol == ':');
            if (ColonCount == 0)
                AddButton.IsEnabled = HostnameRegex.IsMatch(NewText) || IPRegex.IsMatch(NewText);
            else if (ColonCount == 1)
            {
                int ColonIndex = NewText.IndexOf(':');
                string Host = NewText.Substring(0, ColonIndex);
                AddButton.IsEnabled = (HostnameRegex.IsMatch(Host) || IPRegex.IsMatch(Host)) && ushort.TryParse(NewText.Substring(++ColonIndex), out _);
            }
            else
                AddButton.IsEnabled = false;
        }
        private void TextInputHandler(object Sender, TextCompositionEventArgs Args) => Args.Handled = InvalidChar(Args.Text[0]);
        private bool InvalidChar(char Symbol)
        {
            int Index = ToInt32(Symbol);
            return !(Index == 45 || Index == 46 || Index > 47 && Index < 59 || Index > 64 && Index < 91 || Index > 96 && Index < 123);
        }
    }
}