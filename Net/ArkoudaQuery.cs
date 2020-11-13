using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using TEKLauncher.ARK;
using TEKLauncher.Controls;
using TEKLauncher.Data;
using TEKLauncher.Pages;
using TEKLauncher.Servers;
using static System.Array;
using static System.BitConverter;
using static System.DateTime;
using static System.IO.File;
using static System.Text.Encoding;
using static System.Threading.Tasks.Task;
using static System.Windows.Application;
using static TEKLauncher.App;
using static TEKLauncher.Servers.ClustersManager;

namespace TEKLauncher.Net
{
    internal class ArkoudaQuery
    {
        private IPAddress ArkoudaIP;
        internal readonly Dictionary<MapCode, byte[]> Checksums = new Dictionary<MapCode, byte[]>(9);
        private void RefreshClusterPage()
        {
            if (Instance.CurrentPage is ClusterPage Page && IndexOf(Clusters, Page.Cluster) == 0)
            {
                Page.ServersList.Children.Clear();
                foreach (Server Server in Page.Cluster.Servers)
                    Page.ServersList.Children.Add(new ServerItem(Server));
            }
        }
        private Server ReadServer(MemoryStream Stream)
        {
            MapCode Map = (MapCode)Stream.ReadByte();
            byte[] Buffer = new byte[2];
            Stream.Read(Buffer, 0, 2);
            int Port = ToUInt16(Buffer, 0), PlayersCount = Stream.ReadByte(), CustomNameLength = Stream.ReadByte();
            if (PlayersCount == 255)
                PlayersCount = -1;
            string CustomName = null;
            if (CustomNameLength != 0)
            {
                Stream.Read(Buffer = new byte[CustomNameLength], 0, CustomNameLength);
                CustomName = UTF8.GetString(Buffer);
            }
            Server Server = new Server(ArkoudaIP, Map, Port, CustomName);
            Server.Refresh(PlayersCount);
            return Server;
        }
        internal bool Request()
        {
            byte[] Query = new Downloader().TryDownloadData(Links.ArkoudaQuery);
            if (Query is null)
                return false;
            try
            {
                using (MemoryStream Stream = new MemoryStream(Query))
                {
                    byte[] Buffer = new byte[4];
                    Stream.Read(Buffer, 0, 4);
                    ArkoudaIP = new IPAddress(Buffer);
                    int ServersCount = Stream.ReadByte();
                    Clusters[0].Servers = new Server[ServersCount];
                    for (int Iterator = 0; Iterator < ServersCount; Iterator++)
                        Clusters[0].Servers[Iterator] = ReadServer(Stream);
                    Stream.Position++;
                    Stream.Read(Buffer = new byte[8], 0, 8);
                    if (UtcNow.Ticks > ToInt64(Buffer, 0))
                        foreach (Server Server in Clusters[0].Servers)
                            Server.Refresh(-1);
                    Current.Dispatcher.Invoke(RefreshClusterPage);
                    for (MapCode Iterator = MapCode.TheIsland; Stream.Position < Stream.Length; Iterator++)
                    {
                        Buffer = new byte[20];
                        Stream.Read(Buffer, 0, 20);
                        Checksums.Add((MapCode)Iterator, Buffer);
                    }
                }
                return true;
            }
            catch (Exception Failure)
            {
                WriteAllText($@"{AppDataFolder}\QueryFailure.txt", $"{Failure.Message}\n{Failure.StackTrace}");
                return false;
            }
        }
        internal Task<bool> RequestAsync() => Run(Request);
    }
}