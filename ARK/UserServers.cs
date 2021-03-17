using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Xml;
using TEKLauncher.Net;
using TEKLauncher.Servers;
using static System.BitConverter;
using static System.Convert;
using static System.IO.File;
using static System.Net.Dns;
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
        private static readonly Dictionary<string, IPAddress> ResolvedIPs = new Dictionary<string, IPAddress>();
        internal static readonly List<KeyValuePair<Server, string>> UServers = new List<KeyValuePair<Server, string>>();
        private static Server FindServer(IPAddress IP, int GamePort)
        {
            byte[] ServersList = new Downloader().TryDownloadData($"{SteamWebAPI}ISteamApps/GetServersAtAddress/v0001?addr={IP}&format=xml");
            if (ServersList is null)
                return null;
            XmlDocument Document = new XmlDocument();
            using (MemoryStream Stream = new MemoryStream(ServersList))
                Document.Load(Stream);
            XmlNodeList Items = Document.DocumentElement.ChildNodes;
            if (Items[0].InnerText != "true")
                return null;
            Items = Items[1].ChildNodes;
            foreach (XmlNode Item in Items)
            {
                XmlNodeList Fields = Item.ChildNodes;
                if (!int.TryParse(Fields[7].InnerText, out int Result) || Result != GamePort)
                    continue;
                if (!int.TryParse(Fields[2].InnerText, out Result) || Result != 480)
                    return null;
                string Address = Fields[0].InnerText;
                if (!int.TryParse(Address.Substring(Address.IndexOf(':') + 1), out int Port))
                    return null;
                return ResolveServer(IP, Port);
            }
            return null;
        }
        private static Server FindServer(object Args)
        {
            object[] ArgsArray = (object[])Args;
            return FindServer((IPAddress)ArgsArray[0], (int)ArgsArray[1]);
        }
        private static Server ResolveServer(IPAddress IP, int Port)
        {
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
                    bool NS = false;
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
                        byte[] Buffer = new byte[2];
                        Reader.Read(Buffer, 0, 2);
                        if (ToInt16(Buffer, 0) != 480)
                            NS = true;
                        Reader.Position++;
                        MaxPlayers = Reader.ReadByte();
                    }
                    MapCode Code = MapCode.TheIsland;
                    foreach (DLC DLC in DLCs)
                        if (Map.Contains(DLC.Name))
                            Code = DLC.Code;
                    if (Name.Length > 27)
                        Name = Name.Substring(0, 27);
                    return new Server(IP, NS ? (MapCode)(-1) : Code, Port, Name) { MaxPlayers = MaxPlayers };
                }
                catch { return null; }
            }
        }
        private static Server ResolveServer(object Args)
        {
            object[] ArgsArray = (object[])Args;
            return ResolveServer((IPAddress)ArgsArray[0], (int)ArgsArray[1]);
        }
        private static Server[] GetServersForIP(IPAddress IP)
        {
            byte[] ServersList = new Downloader().TryDownloadData($"{SteamWebAPI}ISteamApps/GetServersAtAddress/v0001?addr={IP}&format=xml");
            if (ServersList is null)
                return null;
            XmlDocument Document = new XmlDocument();
            using (MemoryStream Stream = new MemoryStream(ServersList))
                Document.Load(Stream);
            XmlNodeList Items = Document.DocumentElement.ChildNodes;
            if (Items[0].InnerText != "true")
                return null;
            Items = Items[1].ChildNodes;
            List<Server> Servers = new List<Server>();
            foreach (XmlNode Item in Items)
            {
                XmlNodeList Fields = Item.ChildNodes;
                if (!int.TryParse(Fields[2].InnerText, out int Result) || Result != 480)
                    return null;
                string Address = Fields[0].InnerText;
                if (!int.TryParse(Address.Substring(Address.IndexOf(':') + 1), out int Port))
                    return null;
                Server Server = ResolveServer(IP, Port);
                if (!(Server is null))
                    Servers.Add(Server);
            }
            if (Servers.Count == 0)
                return null;
            return Servers.ToArray();
        }
        private static Server[] GetServersForIP(object IP) => GetServersForIP((IPAddress)IP);
        internal static void CommitList()
        {
            Clusters[6].Servers = new Server[UServers.Count];
            int Iterator = 0;
            foreach (KeyValuePair<Server, string> UServer in UServers)
                Clusters[6].Servers[Iterator++] = UServer.Key;
        }
        internal static void LoadList(object State)
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
                        int MaxPlayers = Reader.ReadByte(), StringLength = Reader.ReadByte();
                        byte[] StringBuffer = new byte[StringLength];
                        Reader.Read(StringBuffer, 0, StringLength);
                        string Hostname = null, Name = UTF8.GetString(StringBuffer);
                        if (Address.GetAddressBytes().All(Byte => Byte == 0))
                        {
                            Reader.Read(StringBuffer = new byte[StringLength = Reader.ReadByte()], 0, StringLength);
                            Hostname = UTF8.GetString(StringBuffer);
                            if (ResolvedIPs.ContainsKey(Hostname))
                                Address = ResolvedIPs[Hostname];
                            else
                                try
                                {
                                    Address = GetHostAddresses(Hostname)[0];
                                    ResolvedIPs.Add(Hostname, Address);
                                }
                                catch { }
                        }
                        UServers.Add(new KeyValuePair<Server, string>(new Server(Address, Code, Port, Name) { MaxPlayers = MaxPlayers }, Hostname));
                    }
                    CommitList();
                }
        }
        internal static void SaveList()
        {
            using (FileStream Writer = Create(UServersFile))
            {
                Writer.Write(GetBytes((short)UServers.Count), 0, 2);
                foreach (KeyValuePair<Server, string> UServer in UServers)
                    UServer.Key.WriteToFile(Writer, UServer.Value);
            }
        }
        internal static Task<Server> FindServerAsync(IPAddress IP, int GamePort) => Factory.StartNew(FindServer, new object[] { IP, GamePort });
        internal static Task<Server> ResolveServerAsync(IPAddress IP, int Port) => Factory.StartNew(ResolveServer, new object[] { IP, Port });
        internal static Task<Server[]> GetServersForIPAsync(IPAddress IP) => Factory.StartNew(GetServersForIP, IP);
    }
}