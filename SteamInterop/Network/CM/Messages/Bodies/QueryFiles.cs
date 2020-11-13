using System.Collections.Generic;
using System.IO;
using TEKLauncher.Utils;

namespace TEKLauncher.SteamInterop.Network.CM.Messages.Bodies
{
    internal class QueryFiles : ProtoObject
    {
        internal int Page, Total;
        internal string Search;
        internal List<ItemDetails> Details = new List<ItemDetails>();
        protected private override int[] Indexes => new[] { 1, 2 };
        protected private override void ReadField(int Index, Stream Stream)
        {
            if (Index == 1)
                Total = (int)ReadVarInt(Stream);
            else
                Details.Add(ReadProtoObject<ItemDetails>(Stream));
        }
        internal override void Serialize(MemoryStream Stream)
        {
            WriteKey(2, WireType.VarInt, Stream);
            WriteVarUInt((ulong)Page, Stream);
            WriteKey(3, WireType.VarInt, Stream);
            WriteVarUInt(20UL, Stream);
            WriteKey(5, WireType.VarInt, Stream);
            WriteVarUInt(346110UL, Stream);
            if (!(Search is null))
            {
                WriteKey(11, WireType.LengthDelimited, Stream);
                WriteString(Search, Stream);
            }
            WriteKey(32, WireType.VarInt, Stream);
            Stream.WriteByte(1);
        }
    }
}