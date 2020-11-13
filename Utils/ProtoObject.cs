using System.IO;
using System.Linq;
using static System.BitConverter;
using static System.Text.Encoding;

namespace TEKLauncher.Utils
{
    internal abstract class ProtoObject
    {
        protected private abstract int[] Indexes { get; }
        internal void Deserialize(byte[] Buffer)
        {
            using (MemoryStream Stream = new MemoryStream(Buffer))
                Deserialize(Stream);
        }
        internal void Deserialize(Stream Stream, long Length = -1L, bool BreakAtLastField = false)
        {
            long TargetPosition = Length == -1L ? Stream.Length : Stream.Position + Length;
            int LastField = Indexes.Last();
            while (Stream.Position != TargetPosition)
            {
                int Key = (int)ReadVarInt(Stream), Index = Key >> 3;
                WireType Type = (WireType)(Key & 7);
                if (Indexes.Contains(Index))
                {
                    ReadField(Index, Stream);
                    if (Index == LastField && BreakAtLastField)
                        break;
                }
                else
                    switch (Type)
                    {
                        case WireType.VarInt: while ((Stream.ReadByte() & 0x80) != 0); break;
                        case WireType.Fixed64: Stream.Position += 8L; break;
                        case WireType.LengthDelimited:
                            long SkipLength = ReadVarInt(Stream);
                            Stream.Position += SkipLength;
                            break;
                        case WireType.Fixed32: Stream.Position += 4L; break;
                    }
            }
        }
        internal byte[] Serialize()
        {
            using (MemoryStream Stream = new MemoryStream())
            {
                Serialize(Stream);
                return Stream.ToArray();
            }
        }
        protected private static void WriteFixedULong(ulong Value, MemoryStream Stream) => Stream.Write(GetBytes(Value), 0, 8);
        protected private static void WriteKey(int Index, WireType Type, MemoryStream Stream) => WriteVarInt((Index << 3) | (int)Type, Stream);
        protected private static void WriteProtoObject<T>(T Value, MemoryStream Stream) where T : ProtoObject
        {
            using (MemoryStream Serializer = new MemoryStream())
            {
                Value.Serialize(Serializer);
                WriteVarInt(Serializer.Length, Stream);
                Serializer.WriteTo(Stream);
            }
        }
        protected private static void WriteString(string Value, MemoryStream Stream)
        {
            byte[] String = UTF8.GetBytes(Value);
            WriteVarInt(String.LongLength, Stream);
            Stream.Write(String, 0, String.Length);
        }
        protected private static void WriteVarInt(long Value, MemoryStream Stream)
        {
            while (Value > 0x7F)
            {
                Stream.WriteByte((byte)(Value | 0x80));
                Value >>= 7;
            }
            Stream.WriteByte((byte)Value);
        }
        protected private static void WriteVarUInt(ulong Value, MemoryStream Stream)
        {
            while (Value > 0x7FU)
            {
                Stream.WriteByte((byte)(Value | 0x80U));
                Value >>= 7;
            }
            Stream.WriteByte((byte)Value);
        }
        protected private static byte[] ReadByteArray(Stream Stream)
        {
            byte[] Array = new byte[ReadVarInt(Stream)];
            Stream.Read(Array, 0, Array.Length);
            return Array;
        }
        protected private static long ReadVarInt(Stream Stream)
        {
            int Shift = 0;
            long CurrentByte, Result = 0L;
            do
            {
                CurrentByte = Stream.ReadByte();
                Result |= (CurrentByte & 0x7FL) << Shift;
                Shift += 7;
            } while ((CurrentByte & 0x80L) != 0L);
            return Result;
        }
        protected private static uint ReadFixedUInt(Stream Stream)
        {
            byte[] UInt = new byte[4];
            Stream.Read(UInt, 0, 4);
            return ToUInt32(UInt, 0);
        }
        protected private static ulong ReadFixedULong(Stream Stream)
        {
            byte[] ULong = new byte[8];
            Stream.Read(ULong, 0, 8);
            return ToUInt64(ULong, 0);
        }
        protected private static ulong ReadVarUInt(Stream Stream)
        {
            int Shift = 0;
            ulong CurrentByte, Result = 0UL;
            do
            {
                CurrentByte = (ulong)Stream.ReadByte();
                Result |= (CurrentByte & 0x7FUL) << Shift;
                Shift += 7;
            } while ((CurrentByte & 0x80UL) != 0UL);
            return Result;
        }
        protected private static string ReadString(Stream Stream)
        {
            byte[] Array = new byte[ReadVarInt(Stream)];
            Stream.Read(Array, 0, Array.Length);
            return UTF8.GetString(Array);
        }
        protected private static T ReadProtoObject<T>(Stream Stream) where T : ProtoObject, new()
        {
            T Object = new T();
            long ObjectLength = ReadVarInt(Stream);
            Object.Deserialize(Stream, ObjectLength);
            return Object;
        }
        protected private abstract void ReadField(int Index, Stream Stream);
        internal abstract void Serialize(MemoryStream Stream);
    }
}