using System.IO;
using TEKLauncher.Utils;

namespace TEKLauncher.SteamInterop.Network.CM.Messages.Bodies
{
    internal class WorkshopItem : ProtoObject
    {
        internal ulong ItemID, ManifestID;
        protected private override int[] Indexes => new[] { 1, 3 };
        protected private override void ReadField(int Index, Stream Stream)
        {
            if (Index == 1)
                ItemID = ReadFixedULong(Stream);
            else
                ManifestID = ReadFixedULong(Stream);
        }
        internal override void Serialize(MemoryStream Stream)
        {
            WriteKey(1, WireType.Fixed64, Stream);
            WriteFixedULong(ItemID, Stream);
        }
    }
}