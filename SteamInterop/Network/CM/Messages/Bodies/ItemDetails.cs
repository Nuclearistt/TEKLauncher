using System.IO;
using TEKLauncher.Utils;
using static System.DateTimeOffset;

namespace TEKLauncher.SteamInterop.Network.CM.Messages.Bodies
{
    internal class ItemDetails : ProtoObject
    {
        internal int AppID, Result;
        internal long LastUpdated;
        internal ulong ID, Size;
        internal string Name, PreviewURL;
        protected private override int[] Indexes => new[] { 1, 2, 5, 8, 11, 16, 20 };
        protected private override void ReadField(int Index, Stream Stream)
        {
            switch (Index)
            {
                case 1: Result = (int)ReadVarInt(Stream); break;
                case 2: ID = ReadVarUInt(Stream); break;
                case 5: AppID = (int)ReadVarInt(Stream); break;
                case 8: Size = ReadVarUInt(Stream); break;
                case 11: PreviewURL = ReadString(Stream); break;
                case 16: Name = ReadString(Stream); break;
                case 20: LastUpdated = ReadVarInt(Stream); break;
            }
        }
        internal override void Serialize(MemoryStream Stream) { }
    }
}