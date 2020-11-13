using System.Collections.Generic;
using System.IO;
using TEKLauncher.Utils;
using static System.BitConverter;
using static System.Text.Encoding;

namespace TEKLauncher.SteamInterop.Network.Manifest
{
    internal class FileEntry : ProtoObject
    {
        internal bool IsSliced;
        internal byte[] Checksum;
        internal int Flags;
        internal long Size;
        internal string Name;
        internal List<ChunkEntry> Chunks = new List<ChunkEntry>();
        protected private override int[] Indexes => new[] { 1, 2, 3, 5, 6 };
        internal void ReadFromFile(FileStream File)
        {
            File.Read(Checksum = new byte[20], 0, 20);
            byte[] Buffer = new byte[8];
            File.Read(Buffer, 0, 8);
            Size = ToInt64(Buffer, 0);
            File.Read(Buffer, 0, 2);
            byte[] NameBuffer = new byte[ToInt16(Buffer, 0)];
            File.Read(NameBuffer, 0, NameBuffer.Length);
            Name = UTF8.GetString(NameBuffer);
            IsSliced = File.ReadByte() == 1;
            File.Read(Buffer, 0, 4);
            int ChunksCount = ToInt32(Buffer, 0);
            Chunks = new List<ChunkEntry>(ChunksCount);
            for (; ChunksCount > 0; ChunksCount--)
            {
                ChunkEntry Chunk = new ChunkEntry();
                File.Read(Chunk.GID = new byte[20], 0, 20);
                File.Read(Buffer, 0, 4);
                Chunk.CompressedSize = ToInt32(Buffer, 0);
                File.Read(Buffer, 0, 4);
                Chunk.UncompressedSize = ToInt32(Buffer, 0);
                File.Read(Buffer, 0, 4);
                Chunk.Checksum = ToUInt32(Buffer, 0);
                File.Read(Buffer, 0, 8);
                Chunk.Offset = ToInt64(Buffer, 0);
                Chunks.Add(Chunk);
            }
        }
        internal void WriteToFile(FileStream File)
        {
            File.Write(Checksum, 0, 20);
            File.Write(GetBytes(Size), 0, 8);
            byte[] NameBuffer = UTF8.GetBytes(Name);
            File.Write(GetBytes((short)NameBuffer.Length), 0, 2);
            File.Write(NameBuffer, 0, NameBuffer.Length);
            File.WriteByte((byte)(IsSliced ? 1 : 0));
            File.Write(GetBytes(Chunks.Count), 0, 4);
            foreach (ChunkEntry Chunk in Chunks)
            {
                File.Write(Chunk.GID, 0, 20);
                File.Write(GetBytes(Chunk.CompressedSize), 0, 4);
                File.Write(GetBytes(Chunk.UncompressedSize), 0, 4);
                File.Write(GetBytes(Chunk.Checksum), 0, 4);
                File.Write(GetBytes(Chunk.Offset), 0, 8);
            }
        }
        protected private override void ReadField(int Index, Stream Stream)
        {
            switch (Index)
            {
                case 1: Name = ReadString(Stream); break;
                case 2: Size = ReadVarInt(Stream); break;
                case 3: Flags = (int)ReadVarInt(Stream); break;
                case 5: Checksum = ReadByteArray(Stream); break;
                case 6: Chunks.Add(ReadProtoObject<ChunkEntry>(Stream)); break;
            }
        }
        internal override void Serialize(MemoryStream Stream) { }
    }
}