using System.IO;
using System.Net;
using System.Net.Sockets;
using TEKLauncher.ARK;
using static System.BitConverter;
using static System.Text.Encoding;
using static TEKLauncher.ARK.DLCManager;

namespace TEKLauncher.Servers
{
    internal class Server
    {
        internal Server(IPAddress IP, MapCode Code, int Port, string CustomName = null)
        {
            Endpoint = new IPEndPoint(IP, Port);
            this.CustomName = CustomName;
            this.Code = Code;
        }
        private IPEndPoint Endpoint;
        internal bool IsLoaded = false, IsOnline = false;
        internal int MaxPlayers, PlayersOnline = 0;
        private readonly string CustomName;
        internal readonly MapCode Code;
        internal string ConnectionLine => $"+connect {Endpoint.Address}:{Endpoint.Port}";
        internal string Name => CustomName ?? (Code == MapCode.TheIsland ? "The Island" : GetDLC(Code).Name);
        private int GetPlayersCount()
        {
            using (UdpClient Client = new UdpClient())
            {
                Client.Client.SendTimeout = Client.Client.ReceiveTimeout = 2000;
                try
                {
                    Client.Connect(Endpoint);
                    byte[] Datagram = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x55, 0xFF, 0xFF, 0xFF, 0xFF };
                    Client.Send(Datagram, 9);
                    byte[] Response = Client.Receive(ref Endpoint);
                    for (int Iterator = 5; Iterator < 9; Iterator++)
                        Datagram[Iterator] = Response[Iterator];
                    Client.Send(Datagram, 9);
                    int Count = 0;
                    using (MemoryStream Reader = new MemoryStream(Client.Receive(ref Endpoint)))
                    {
                        Reader.Position = 6L;
                        while (Reader.Position != Reader.Length)
                        {
                            Reader.Position++;
                            bool ActualPlayer = false;
                            while (Reader.ReadByte() != 0)
                                ActualPlayer = true;
                            if (ActualPlayer)
                                Count++;
                            Reader.Position += 8L;
                        }
                    }
                    return Count;
                }
                catch { return -1; }
            }
        }
        internal void Refresh(int ArkoudaQuery = -2)
        {
            int PlayersCount = ArkoudaQuery == -2 ? GetPlayersCount() : ArkoudaQuery;
            IsOnline = PlayersCount != -1;
            PlayersOnline = PlayersCount == -1 ? 0 : PlayersCount;
            IsLoaded = true;
        }
        internal void WriteToFile(FileStream File)
        {
            File.Write(Endpoint.Address.GetAddressBytes(), 0, 4);
            File.WriteByte((byte)Code);
            File.Write(GetBytes((ushort)Endpoint.Port), 0, 2);
            File.WriteByte((byte)MaxPlayers);
            byte[] EncodedName = UTF8.GetBytes(CustomName);
            File.WriteByte((byte)EncodedName.Length);
            File.Write(EncodedName, 0, EncodedName.Length);
        }
    }
}