using System.IO;
using TEKLauncher.Utils;

namespace TEKLauncher.SteamInterop.Network.CM.Messages.Bodies
{
    internal class ItemInfo : ProtoObject
    {
        internal uint AppID;
        internal WorkshopItem Item;
        protected private override int[] Indexes => new[] { 2 };
        protected private override void ReadField(int Index, Stream Stream) => Item = ReadProtoObject<WorkshopItem>(Stream);
        internal override void Serialize(MemoryStream Stream)
        {
            WriteKey(1, WireType.VarInt, Stream);
            WriteVarUInt(AppID, Stream);
            WriteKey(3, WireType.LengthDelimited, Stream);
            WriteProtoObject(Item, Stream);
        }
    }
}