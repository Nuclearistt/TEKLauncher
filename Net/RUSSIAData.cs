using System.Collections.Generic;
using System.IO;
using System.Net;
using TEKLauncher.ARK;
using TEKLauncher.Controls;
using TEKLauncher.Pages;
using TEKLauncher.Servers;
using static System.Array;
using static System.Net.IPAddress;
using static System.Windows.Application;
using static TEKLauncher.App;
using static TEKLauncher.Data.Links;
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.Servers.ClustersManager;

namespace TEKLauncher.Net
{
    internal static class RUSSIAData
    {
        private static void RefreshClusterPage()
        {
            if (Instance.CurrentPage is ClusterPage Page && IndexOf(Clusters, Page.Cluster) == 5)
            {
                Page.InfoStack.Children.Clear();
                Page.LoadInfo();
                Page.ServersList.Children.Clear();
                foreach (Server Server in Page.Cluster.Servers)
                    Page.ServersList.Children.Add(new ServerItem(Server));
            }
        }
        internal static void LoadServers(object State)
        {
            byte[] Data = new Downloader().TryDownloadData($"{RUSSIA}maps");
            if (Data is null)
                foreach (Server Server in Clusters[5].Servers)
                    Server.Refresh(-1);
            else
            {
                Server[] Servers;
                using (MemoryStream Stream = new MemoryStream(Data))
                using (StreamReader Reader = new StreamReader(Stream))
                {
                    IPAddress IP = Parse(Reader.ReadLine());
                    int Count = int.Parse(Reader.ReadLine());
                    Servers = new Server[Count];
                    for (int Iterator = 0; Iterator < Count; Iterator++)
                    {
                        string[] Fields = Reader.ReadLine().Split();
                        int Port = int.Parse(Fields[0]);
                        MapCode Map = (MapCode)int.Parse(Fields[1]);
                        string CustomName = Fields.Length == 3 ? Fields[2].Replace('+', ' ') : null;
                        Servers[Iterator] = new Server(IP, Map, Port, CustomName);
                    }
                    List<string> Info = new List<string>();
                    while (!Reader.EndOfStream)
                    {
                        string InfoLine = Reader.ReadLine();
                        string[] Fields = InfoLine.Split();
                        if (Fields.Length == 2 && Fields[0][0] == '{' && Fields[0][2] == '}')
                            InfoLine = string.Format(LocString(LocCode.MaxDinoLvl + int.Parse(Fields[0][1].ToString())), float.Parse(Fields[1]));
                        Info.Add(InfoLine);
                    }
                    Clusters[5].Info[string.Empty] = string.Join("\n", Info);
                }
                Sort(Servers, (A, B) => A.Code.CompareTo(B.Code));
                Clusters[5].Servers = Servers;
                Current.Dispatcher.Invoke(RefreshClusterPage);
                foreach (Server Server in Clusters[5].Servers)
                    Server.Refresh();
            }
        }
    }
}