using System.Collections.Generic;
using System.IO;
using TEKLauncher.ARK;
using TEKLauncher.Controls;
using TEKLauncher.Pages;
using TEKLauncher.Servers;
using static System.Array;
using static System.Net.IPAddress;
using static System.Windows.Application;
using static TEKLauncher.App;
using static TEKLauncher.ARK.DLCManager;
using static TEKLauncher.Data.Links;
using static TEKLauncher.Net.Downloader;
using static TEKLauncher.Servers.ClustersManager;

namespace TEKLauncher.Net
{
    internal static class ARKdictedData
    {
        internal static readonly Dictionary<ulong, ulong> Workshop = new Dictionary<ulong, ulong>();
        private static void RefreshMods()
        {
            if (Instance.CurrentPage is ClusterPage Page && IndexOf(Clusters, Page.Cluster) == 1)
            {
                Page.ModsList.Children.Clear();
                Page.LoadMods();
            }
        }
        private static void RefreshServers()
        {
            if (Instance.CurrentPage is ClusterPage Page && IndexOf(Clusters, Page.Cluster) == 1)
            {
                Page.ServersList.Children.Clear();
                foreach (Server Server in Page.Cluster.Servers)
                    Page.ServersList.Children.Add(new ServerItem(Server));
            }
        }
        internal static void LoadMods(object State)
        {
            byte[] Data = TryDownloadData($"{ARKdicted}workshop/modinfo.txt");
            if (Data is null)
                return;
            List<ModRecord> List = new List<ModRecord>();
            using (MemoryStream Stream = new MemoryStream(Data))
            using (StreamReader Reader = new StreamReader(Stream))
                while (!Reader.EndOfStream)
                {
                    string Line = Reader.ReadLine();
                    if (string.IsNullOrEmpty(Line) || Line.IndexOf(',') == -1)
                        continue;
                    string[] Fields = Line.Split(',');
                    if (Fields.Length != 3 || !ulong.TryParse(Fields[0], out ulong ID))
                        continue;
                    List.Add(new ModRecord(ID, Fields[1], Fields[2]));
                }
            Clusters[1].Mods[string.Empty] = List.ToArray();
            Current.Dispatcher.Invoke(RefreshMods);
        }
        internal static void LoadServers(object State)
        {
            try
            {
                byte[] Data = TryDownloadData($"{ARKdicted}workshop/ServersInfo.txt");
                if (Data is null)
                    foreach (Server Server in Clusters[1].Servers)
                        Server.Refresh(-1);
                else
                {
                    List<Server> Servers = new List<Server>();
                    using (MemoryStream Stream = new MemoryStream(Data))
                    using (StreamReader Reader = new StreamReader(Stream))
                        while (!Reader.EndOfStream)
                        {
                            string[] Fields = Reader.ReadLine().Split();
                            string Name = Fields[0].Substring(Fields[0].IndexOf('+') + 1).Replace('+', ' ');
                            if (Fields[1] == "PVP")
                                Name += " PvP";
                            MapCode Map = Name == "The Island" ? MapCode.TheIsland : MapCode.Mod;
                            foreach (DLC DLC in DLCs)
                                if (Name == DLC.Name)
                                    Map = DLC.Code;
                            Servers.Add(new Server(Parse(Fields[2]), Map, int.Parse(Fields[3]), Map == MapCode.Mod ? Name : null));
                        }
                    Servers.Sort((A, B) => A.Code.CompareTo(B.Code));
                    Clusters[1].Servers = Servers.ToArray();
                    Current.Dispatcher.Invoke(RefreshServers);
                    foreach (Server Server in Clusters[1].Servers)
                        Server.Refresh();
                }
            }
            catch { }
        }
        internal static void LoadWorkshop(object State)
        {
            if (Workshop.Count > 0)
                return;
            byte[] Data = TryDownloadData($"{ARKdicted}workshop/ModsIds.txt");
            try
            {
                using (MemoryStream Stream = new MemoryStream(Data))
                using (StreamReader Reader = new StreamReader(Stream))
                    while (!Reader.EndOfStream)
                    {
                        string[] IDs = Reader.ReadLine().Split();
                        Workshop.Add(ulong.Parse(IDs[0]), ulong.Parse(IDs[1]));
                    }
            }
            catch { }
        }
    }
}