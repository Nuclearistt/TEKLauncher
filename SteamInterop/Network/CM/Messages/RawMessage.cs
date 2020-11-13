using System.IO;
using TEKLauncher.SteamInterop.Network.CM.Messages.Bodies;
using static System.BitConverter;

namespace TEKLauncher.SteamInterop.Network.CM.Messages
{
    internal class RawMessage<TBody> where TBody : RawBody, new()
    {
        internal RawMessage(MessageType Type) => this.Type = Type;
        internal RawMessage(byte[] Data)
        {
            using (MemoryStream Stream = new MemoryStream(Data) { Position = 20L })
            {
                Body.Deserialize(Stream);
                int PayloadLength = (int)(Stream.Length - Stream.Position);
                Stream.Read(Payload = new byte[PayloadLength], 0, PayloadLength);
            }
        }
        internal byte[] Payload;
        internal TBody Body = new TBody();
        private readonly MessageType Type;
        internal byte[] Serialize()
        {
            using (MemoryStream Stream = new MemoryStream())
            {
                Stream.Write(GetBytes((int)Type), 0, 4);
                byte[] Buffer = new byte[16];
                for (int Iterator = 0; Iterator < 16; Iterator++)
                    Buffer[Iterator] = 0xFF;
                Stream.Write(Buffer, 0, 16);
                Body.Serialize(Stream);
                if (!(Payload is null))
                    Stream.Write(Payload, 0, Payload.Length);
                return Stream.ToArray();
            }
        }
    }
}