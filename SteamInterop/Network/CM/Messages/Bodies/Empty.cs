using System.IO;
using TEKLauncher.Utils;

namespace TEKLauncher.SteamInterop.Network.CM.Messages.Bodies
{
    internal class Empty : ProtoObject
    {
        protected private override int[] Indexes => null;
        protected private override void ReadField(int Index, Stream Stream) { }
        internal override void Serialize(MemoryStream Stream) { }
    }
}