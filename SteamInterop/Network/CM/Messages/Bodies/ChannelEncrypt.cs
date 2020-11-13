using System.IO;
using static System.BitConverter;

namespace TEKLauncher.SteamInterop.Network.CM.Messages.Bodies
{
    internal class ChannelEncrypt : RawBody
    {
        internal override void Deserialize(MemoryStream Stream) => Stream.Position += 8L;
        internal override void Serialize(MemoryStream Stream)
        {
            Stream.Write(GetBytes(1U), 0, 4);
            Stream.Write(GetBytes(128U), 0, 4);
        }
    }
}