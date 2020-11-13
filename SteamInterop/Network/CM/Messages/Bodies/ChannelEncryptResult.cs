using System.IO;
using static System.BitConverter;

namespace TEKLauncher.SteamInterop.Network.CM.Messages.Bodies
{
    internal class ChannelEncryptResult : RawBody
    {
        internal bool Success;
        internal override void Deserialize(MemoryStream Stream)
        {
            byte[] Buffer = new byte[4];
            Stream.Read(Buffer, 0, 4);
            Success = ToInt32(Buffer, 0) == 1;
        }
        internal override void Serialize(MemoryStream Stream) { }
    }
}