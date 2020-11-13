using System.IO;
using TEKLauncher.Utils;

namespace TEKLauncher.SteamInterop.Network.CM.Messages.Bodies
{
    internal class ProductInfo : ProtoObject
    {
        internal AppInfo App;
        protected private override int[] Indexes => new[] { 1 };
        protected private override void ReadField(int Index, Stream Stream) => App = ReadProtoObject<AppInfo>(Stream);
        internal override void Serialize(MemoryStream Stream)
        {
            WriteKey(2, WireType.LengthDelimited, Stream);
            WriteProtoObject(new AppInfo(), Stream);
            WriteKey(3, WireType.VarInt, Stream);
            Stream.WriteByte(0);
        }
    }
}