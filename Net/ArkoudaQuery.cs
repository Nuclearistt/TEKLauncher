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
using static TEKLauncher.Data.LocalizationManager;
using static TEKLauncher.Net.Downloader;
using static TEKLauncher.Servers.ClustersManager;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.Net
{
    internal class ArkoudaQuery
    {
        private List<IPAddress> IPs;
        internal readonly Dictionary<MapCode, byte[]> Checksums = new Dictionary<MapCode, byte[]>(9);
        internal static string LastUpdated;
        private void ReadHashes(MemoryStream Stream, int Count)
        {
            for (int Iterator = 0; Iterator < Count; Iterator++)
            {
                byte[] Buffer = new byte[20];
                Stream.Read(Buffer, 0, 20);
                Checksums.Add((MapCode)Iterator, Buffer);
            }
        }
        private void ReadIPs(MemoryStream Stream, int Count)
        {
            byte[] Buffer = new byte[4];
            for (; Count > 0; Count--)
            {
                Stream.Read(Buffer, 0, 4);
                IPs.Add(new IPAddress(Buffer));
            }    
        }
        private void ReadMods(MemoryStream Stream)
        {
            int GroupsCount = Stream.ReadByte();
            for (int Iterator = 0; Iterator < GroupsCount; Iterator++)
            {
                int NameSize = Stream.ReadByte();
                byte[] Buffer = new byte[NameSize];
                Stream.Read(Buffer, 0, NameSize);
                Clusters[0].Mods.Add(UTF8.GetString(Buffer), ReadModsList(Stream));
            }
            Clusters[1].Mods.Add(string.Empty, ReadModsList(Stream));
            Current.Dispatcher.Invoke(RefreshMods);
        }
        private void ReadServers(MemoryStream Stream)
        {
            int ServersCount = Stream.ReadByte();
            Clusters[0].Servers = new Server[ServersCount];
            for (int Iterator = 0; Iterator < ServersCount; Iterator++)
                Clusters[0].Servers[Iterator] = ReadServer(Stream);
            ServersCount = Stream.ReadByte();
            Clusters[1].Servers = new Server[ServersCount];
            for (int Iterator = 0; Iterator < ServersCount; Iterator++)
                Clusters[1].Servers[Iterator] = ReadServer(Stream);
            Current.Dispatcher.Invoke(RefreshServers);
        }
        private void RefreshMods()
        {
            if (Instance.CurrentPage is ClusterPage Page && IndexOf(Clusters, Page.Cluster) < 2)
            {
                Page.ModsList.Children.Clear();
                Page.LoadMods();
            }
        }
        private void RefreshServers()
        {
            if (Instance.CurrentPage is ClusterPage Page && IndexOf(Clusters, Page.Cluster) < 2)
            {
                Page.LastUpdated.Text = LastUpdated;
                Page.ServersList.Children.Clear();
                foreach (Server Server in Page.Cluster.Servers)
                    Page.ServersList.Children.Add(new ServerItem(Server));
            }
        }
        private ModRecord[] ReadModsList(MemoryStream Stream)
        {
            int Count = Stream.ReadByte();
            ModRecord[] List = new ModRecord[Count];
            for (int Iterator = 0; Iterator < Count; Iterator++)
            {
                byte[] Buffer = new byte[8];
                Stream.Read(Buffer, 0, 8);
                ulong ID = ToUInt64(Buffer, 0);
                int StringSize = Stream.ReadByte();
                Stream.Read(Buffer = new byte[StringSize], 0, StringSize);
                string Name = UTF8.GetString(Buffer);
                Stream.Read(Buffer = new byte[StringSize = Stream.ReadByte()], 0, StringSize);
                string Size = UTF8.GetString(Buffer);
                List[Iterator] = new ModRecord(ID, Name, Size);
            }
            return List;
        }
        private Server ReadServer(MemoryStream Stream)
        {
            int IPIndex = Stream.ReadByte();
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
            Server Server = new Server(IPs[IPIndex], Map, Port, CustomName);
            Server.Refresh(PlayersCount);
            return Server;
        }
        internal bool Request()
        {
            byte[] Query = TryDownloadData(Links.ArkoudaQuery);
            if (Query is null)
                return false;
            try
            {
                using (MemoryStream Reader = new MemoryStream(Query))
                {
                    byte[] Buffer = new byte[8];
                    Reader.Read(Buffer, 0, 8);
                    LastUpdated = string.Format(LocString(LocCode.LastUpdated), ConvertTime((UtcNow.Ticks - ToInt64(Buffer, 0)) / 10000000L));
                    int SectionIndex;
                    while ((SectionIndex = Reader.ReadByte()) != -1)
                    {
                        Reader.Read(Buffer, 0, 2);
                        int BufferSize = ToInt16(Buffer, 0);
                        byte[] DataBuffer = new byte[BufferSize];
                        Reader.Read(DataBuffer, 0, BufferSize);
                        switch (SectionIndex)
                        {
                            case 0:
                                int IPsCount = BufferSize / 4;
                                IPs = new List<IPAddress>(IPsCount);
                                using (MemoryStream Stream = new MemoryStream(DataBuffer))
                                    try { ReadIPs(Stream, IPsCount); }
                                    catch { }
                                break;
                            case 1:
                                using (MemoryStream Stream = new MemoryStream(DataBuffer))
                                    try { ReadServers(Stream); }
                                    catch { }
                                break;
                            case 2:
                                using (MemoryStream Stream = new MemoryStream(DataBuffer))
                                    try { ReadHashes(Stream, BufferSize / 20); }
                                    catch { }
                                break;
                            case 3:
                                using (MemoryStream Stream = new MemoryStream(DataBuffer))
                                    try { ReadMods(Stream); }
                                    catch { }
                                break;
                        }
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