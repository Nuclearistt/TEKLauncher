using System.IO;
using TEKLauncher.Utils;
using static System.BitConverter;

namespace TEKLauncher.SteamInterop.Network.CM.Messages
{
    internal class Message
    {
        internal Message(MessageType Type) => this.Type = Type;
        protected ProtoObject BodyObject;
        private readonly MessageType Type;
        internal readonly MessageHeader Header = new MessageHeader();
        internal byte[] Serialize()
        {
            using (MemoryStream Stream = new MemoryStream())
            using (BinaryWriter Writer = new BinaryWriter(Stream))
            {
                Writer.Write((uint)Type | 0x80000000U);
                using (MemoryStream Serializer = new MemoryStream())
                {
                    Header.Serialize(Serializer);
                    Writer.Write((int)Serializer.Length);
                    Serializer.WriteTo(Stream);
                }
                BodyObject.Serialize(Stream);
                return Stream.ToArray();
            }
        }
    }
    internal class Message<TBody> : Message where TBody : ProtoObject, new()
    {
        internal Message(MessageType Type) : base(Type) => BodyObject = new TBody();
        internal Message(byte[] Data) : base(MessageType.Invalid)
        {
            BodyObject = new TBody();
            using (MemoryStream Stream = new MemoryStream(Data))
            {
                Stream.Position += 4L;
                byte[] Buffer = new byte[4];
                Stream.Read(Buffer, 0, 4);
                long HeaderLength = ToInt32(Buffer, 0);
                Header.Deserialize(Stream, HeaderLength);
                BodyObject.Deserialize(Stream, -1L, true);
            }
        }
        internal TBody Body => (TBody)BodyObject;
    }
}