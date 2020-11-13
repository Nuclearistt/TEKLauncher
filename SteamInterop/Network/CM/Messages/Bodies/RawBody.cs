using System.IO;

namespace TEKLauncher.SteamInterop.Network.CM.Messages.Bodies
{
    internal abstract class RawBody
    {
        internal abstract void Deserialize(MemoryStream Stream);
        internal abstract void Serialize(MemoryStream Stream);
    }
}