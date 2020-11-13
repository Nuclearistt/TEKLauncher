using System.IO;
using TEKLauncher.Utils;

namespace TEKLauncher.SteamInterop.Network.CM.Messages.Bodies
{
    internal class Multi : ProtoObject
    {
        internal byte[] MessageBody;
        internal int? UncompressedSize;
        protected private override int[] Indexes => new[] { 1, 2 };
        protected private override void ReadField(int Index, Stream Stream)
        {
            switch (Index)
            {
                case 1: UncompressedSize = (int?)ReadVarInt(Stream); break;
                case 2: MessageBody = ReadByteArray(Stream); break;
            }
        }
        internal override void Serialize(MemoryStream Stream) { }
    }
}