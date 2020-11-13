using System.IO;
using TEKLauncher.Utils;

namespace TEKLauncher.SteamInterop.Network.CM.Messages.Bodies
{
    internal class AppInfo : ProtoObject
    {
        internal byte[] Buffer;
        protected private override int[] Indexes => new[] { 5 };
        protected private override void ReadField(int Index, Stream Stream) => Buffer = ReadByteArray(Stream);
        internal override void Serialize(MemoryStream Stream)
        {
            WriteKey(1, WireType.VarInt, Stream);
            WriteVarUInt(346110U, Stream);
            WriteKey(2, WireType.VarInt, Stream);
            WriteVarUInt(0UL, Stream);
            WriteKey(3, WireType.VarInt, Stream);
            Stream.WriteByte(0);
        }
    }
}