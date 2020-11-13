using System.IO;
using TEKLauncher.Utils;

namespace TEKLauncher.SteamInterop.Network.CM.Messages.Bodies
{
    internal class ServiceMethod : ProtoObject
    {
        internal byte[] SerializedMethod;
        internal string MethodName;
        protected private override int[] Indexes => new[] { 2 };
        protected private override void ReadField(int Index, Stream Stream) => SerializedMethod = ReadByteArray(Stream);
        internal override void Serialize(MemoryStream Stream)
        {
            WriteKey(1, WireType.LengthDelimited, Stream);
            WriteString(MethodName, Stream);
            WriteKey(2, WireType.LengthDelimited, Stream);
            WriteVarInt(SerializedMethod.Length, Stream);
            Stream.Write(SerializedMethod, 0, SerializedMethod.Length);
            WriteKey(3, WireType.VarInt, Stream);
            Stream.WriteByte(0);
        }
    }
}