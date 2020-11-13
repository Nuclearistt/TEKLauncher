using System.IO;
using System.IO.Compression;
using TEKLauncher.SteamInterop.Network;
using static System.IO.File;

namespace TEKLauncher.Utils
{
    internal static class Zip
    {
        private static byte[] Decompress(Stream Stream, string ItemName)
        {
            byte[] DecompressedData;
            using (BinaryReader Reader = new BinaryReader(Stream))
            {
                if (Reader.ReadUInt32() != 0x04034B50U)
                    throw new ValidatorException($"Failed to decompress {ItemName}");
                Stream.Position += 4L;
                ushort Method = Reader.ReadUInt16();
                if (Method != 0U && Method != 8U)
                    throw new ValidatorException($"Failed to decompress {ItemName}");
                Stream.Position += 8L;
                int CompressedSize = Reader.ReadInt32(), UncompressedSize = Reader.ReadInt32();
                long SkipInterval = (long)Reader.ReadUInt16() + Reader.ReadUInt16();
                Stream.Position += SkipInterval;
                long ReadPosition = Stream.Position;
                Stream.Position += CompressedSize;
                if (Reader.ReadUInt32() != 0x02014B50U)
                    throw new ValidatorException($"Failed to decompress {ItemName}");
                Stream.Position += 6L;
                uint Compression = Reader.ReadUInt16();
                if (Compression != 0U && Compression != 8U)
                    throw new ValidatorException($"Failed to decompress {ItemName}");
                Stream.Position += 16L;
                long SkipSpan = (long)Reader.ReadUInt16() + Reader.ReadUInt16() + Reader.ReadUInt16();
                Stream.Position += 12L + SkipSpan;
                if (Reader.ReadUInt32() != 0x06054B50)
                    throw new ValidatorException($"Failed to decompress {ItemName}");
                Stream.Position = ReadPosition;
                Stream Decompresser = Method == 8U ? new DeflateStream(Stream, CompressionMode.Decompress) : Stream;
                Decompresser.Read(DecompressedData = new byte[UncompressedSize], 0, UncompressedSize);
                Decompresser.Dispose();
            }
            return DecompressedData;
        }
        internal static void Decompress(string SourcePath, string DestinationPath)
        {
            byte[] DecompressedData;
            using (FileStream Stream = OpenRead(SourcePath))
                DecompressedData = Decompress(Stream, "manifest");
            using (FileStream DecompressedFile = Create(DestinationPath))
                DecompressedFile.Write(DecompressedData, 0, DecompressedData.Length);
        }
        internal static byte[] Decompress(byte[] Input)
        {
            byte[] DecompressedData;
            using (MemoryStream Stream = new MemoryStream(Input))
                DecompressedData = Decompress(Stream, "chunk");
            return DecompressedData;
        }
    }
}