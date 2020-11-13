using System.Collections.Generic;
using System.IO;
using TEKLauncher.Utils;

namespace TEKLauncher.SteamInterop.Network.CM.Messages.Bodies
{
    internal class GetDetails : ProtoObject
    {
        internal ulong[] IDs;
        internal List<ItemDetails> Details = new List<ItemDetails>();
        protected private override int[] Indexes => new[] { 1 };
        protected private override void ReadField(int Index, Stream Stream) => Details.Add(ReadProtoObject<ItemDetails>(Stream));
        internal override void Serialize(MemoryStream Stream)
        {
            foreach (ulong ID in IDs)
            {
                WriteKey(1, WireType.Fixed64, Stream);
                WriteFixedULong(ID, Stream);
            }
            WriteKey(11, WireType.VarInt, Stream);
            Stream.WriteByte(1);
        }
    }
}