using System.IO;
using TEKLauncher.Utils;

namespace TEKLauncher.SteamInterop.Network.CM.Messages
{
    internal class MessageHeader : ProtoObject
    {
        internal int? SessionID;
        internal ulong? SourceJobID, SteamID;
        protected private override int[] Indexes => new[] { 1, 2 };
        protected private override void ReadField(int Index, Stream Stream)
        {
            switch (Index)
            {
                case 1: SteamID = ReadFixedULong(Stream); break;
                case 2: SessionID = (int?)ReadVarInt(Stream); break;
            }
        }
        internal override void Serialize(MemoryStream Stream)
        {
            if (SteamID.HasValue)
            {
                WriteKey(1, WireType.Fixed64, Stream);
                WriteFixedULong(SteamID.Value, Stream);
            }
            if (SessionID.HasValue)
            {
                WriteKey(2, WireType.VarInt, Stream);
                WriteVarInt(SessionID.Value, Stream);
            }
            if (SourceJobID.HasValue)
            {
                WriteKey(10, WireType.Fixed64, Stream);
                WriteFixedULong(SourceJobID.Value, Stream);
            }
        }
    }
}