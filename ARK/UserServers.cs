using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Xml;
using TEKLauncher.Net;
using TEKLauncher.Servers;
using static System.BitConverter;
using static System.Convert;
using static System.IO.File;
using static System.Text.Encoding;
using static System.Threading.Tasks.Task;
using static TEKLauncher.App;
using static TEKLauncher.ARK.DLCManager;
using static TEKLauncher.Data.Links;
using static TEKLauncher.Servers.ClustersManager;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.ARK
{
    internal static class UserServers
    {
        private static readonly string UServersFile = $@"{AppDataFolder}\UserServers.bin";
        internal static readonly List<Server> UServers = new List<Server>();
        private static Server[] GetServersForIP(object IP) => GetServersForIP((IPAddress)IP);
        internal static void LoadList()
        {
            if (FileExists(UServersFile))
                using (FileStream Reader = OpenRead(UServersFile))
                {
                    byte[] Buffer = new byte[4];
                    Reader.Read(Buffer, 0, 2);
                    int Count = ToInt16(Buffer, 0);
                    for (int Iterator = 0; Iterator < Count; Iterator++)
                    {
                        Reader.Read(Buffer, 0, 4);
                        IPAddress Address = new IPAddress(Buffer);
                        MapCode Code = (MapCode)Reader.ReadByte();
                        Reader.Read(Buffer, 0, 2);
                        int Port = ToUInt16(Buffer, 0);
                        int MaxPlayers = Reader.ReadByte(), NameLength = Reader.ReadByte();
                        byte[] NameBuffer = new byte[NameLength];
                        Reader.Read(NameBuffer, 0, NameLength);
                        UServers.Add(new Server(Address, Code, Port, UTF8.GetString(NameBuffer)) { MaxPlayers = MaxPlayers });
                    }
                    Clusters[5].Servers = UServers.ToArray();
                }
        }
        internal static void SaveList()
        {
            using (FileStream Writer = Create(UServersFile))
            {
                Writer.Write(GetBytes((short)UServers.Count), 0, 2);
                foreach (Server UServer in UServers)
                    UServer.WriteToFile(Writer);
            }
        }
        internal static Server[] GetServersForIP(IPAddress IP)
        {
            byte[] CMList = new Downloader().TryDownloadData($"{SteamWebAPI}ISteamApps/GetServersAtAddress/v0001?addr={IP}&format=xml");
            if (CMList is null)
                return null;
            XmlDocument Document = new XmlDocument();
            using (MemoryStream Stream = new MemoryStream(CMList))
                Document.Load(Stream);
            XmlNodeList Items = Document.DocumentElement.ChildNodes;
            if (Items[0].InnerText != "true")
                return null;
            Items = Items[1].ChildNodes;
            List<Server> Servers = new List<Server>();
            foreach (XmlNode Item in Items)
            {
                XmlNodeList Fields = Item.ChildNodes;
                string Address = Fields[0].InnerText;
                if (int.Parse(Fields[2].InnerText) == 480)
                {
                    int Port = int.Parse(Address.Substring(Address.IndexOf(':') + 1));
                    IPEndPoint Endpoint = new IPEndPoint(IP, Port);
                    using (UdpClient Client = new UdpClient())
                    {
                        Client.Client.SendTimeout = Client.Client.ReceiveTimeout = 4000;
                        try
                        {
                            Client.Connect(Endpoint);
                            byte[] Datagram = new byte[]
                            {
                                0xFF, 0xFF, 0xFF, 0xFF, 0x54, 0x53, 0x6F, 0x75,
                                0x72, 0x63, 0x65, 0x20, 0x45, 0x6E, 0x67, 0x69,
                                0x6E, 0x65, 0x20, 0x51, 0x75, 0x65, 0x72, 0x79, 0x00
                            };
                            Client.Send(Datagram, 25);
                            int MaxPlayers;
                            string Map = string.Empty, Name = string.Empty;
                            using (MemoryStream Reader = new MemoryStream(Client.Receive(ref Endpoint)))
                            {
                                Reader.Position = 6L;
                                int Char;
                                while ((Char = Reader.ReadByte()) != 0)
                                    Name += ToChar(Char);
                                while ((Char = Reader.ReadByte()) != 0)
                                    Map += ToChar(Char);
                                while (Reader.ReadByte() != 0);
                                while (Reader.ReadByte() != 0);
                                Reader.Position += 3L;
                                MaxPlayers = Reader.ReadByte();
                            }
                            MapCode Code = MapCode.TheIsland;
                            foreach (DLC DLC in DLCs)
                                if (Map.Contains(DLC.Name))
                                    Code = DLC.Code;
                            if (Name.Length > 27)
                                Name = Name.Substring(0, 27);
                            Servers.Add(new Server(IP, Code, Port, Name) { MaxPlayers = MaxPlayers });
                        }
                        catch { }
                    }
                }
            }
            if (Servers.Count == 0)
                return null;
            return Servers.ToArray();
        }
        internal static Task<Server[]> GetServersForIPAsync(IPAddress IP) => Factory.StartNew(GetServersForIP, IP);
    }
}