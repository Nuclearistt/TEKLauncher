using System.IO;
using TEKLauncher.Utils;
using static System.Environment;
using static TEKLauncher.SteamInterop.Network.CM.CMClient;

namespace TEKLauncher.SteamInterop.Network.CM.Messages.Bodies
{
    internal class LogOn : ProtoObject
    {
        internal int HeartbeatDelay, Result;
        protected private override int[] Indexes => new[] { 1, 2, 7 };
        protected private override void ReadField(int Index, Stream Stream)
        {
            switch (Index)
            {
                case 1: Result = (int)ReadVarInt(Stream); break;
                case 2: HeartbeatDelay = (int)ReadVarInt(Stream); break;
                case 7: CellID = (uint)ReadVarUInt(Stream); break;
            }
        }
        internal override void Serialize(MemoryStream Stream)
        {
            WriteKey(1, WireType.VarInt, Stream);
            WriteVarUInt(65580U, Stream);
            WriteKey(3, WireType.VarInt, Stream);
            WriteVarUInt(CellID, Stream);
            WriteKey(6, WireType.LengthDelimited, Stream);
            WriteString("english", Stream);
            WriteKey(7, WireType.VarInt, Stream);
            WriteVarInt(OSVersion.Version.Major == 6 ? 13L : 16L, Stream);
        }
    }
}