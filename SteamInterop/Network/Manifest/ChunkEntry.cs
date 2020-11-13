using System.IO;
using TEKLauncher.Utils;

namespace TEKLauncher.SteamInterop.Network.Manifest
{
    internal class ChunkEntry : ProtoObject
    {
        internal byte[] GID;
        internal int CompressedSize, UncompressedSize;
        internal uint Checksum;
        internal long Offset;
        protected private override int[] Indexes => new[] { 1, 2, 3, 4, 5 };
        protected private override void ReadField(int Index, Stream Stream)
        {
            switch (Index)
            {
                case 1: GID = ReadByteArray(Stream); break;
                case 2: Checksum = ReadFixedUInt(Stream); break;
                case 3: Offset = ReadVarInt(Stream); break;
                case 4: UncompressedSize = (int)ReadVarInt(Stream); break;
                case 5: CompressedSize = (int)ReadVarInt(Stream); break;
            }
        }
        internal override void Serialize(MemoryStream Stream) { }
    }
}