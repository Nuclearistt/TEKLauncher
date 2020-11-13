using System.IO;
using TEKLauncher.SteamInterop.Network;
using static System.Array;
using static System.BitConverter;

namespace TEKLauncher.Utils.Zlib
{
    internal class ZlibDecompressor
    {
        internal ZlibDecompressor(FileStream Reader, FileStream Writer, long CompressedSize, long UncompressedSize)
        {
            InputAvailableSize = (int)CompressedSize - 2;
            OutputAvailableSize = (int)UncompressedSize;
            BlocksDecoder = new InflateBlocksDecoder(this, Writer);
            this.Reader = Reader;
        }
        internal int InputAvailableSize, OutputAvailableSize;
        private readonly InflateBlocksDecoder BlocksDecoder;
        internal readonly FileStream Reader;
        internal void DecompressChunk()
        {
            int Method = Reader.ReadByte();
            if ((Method & 15) != 8 || (Method >> 4) + 8 > 15 || ((Method << 8) + Reader.ReadByte()) % 31 != 0)
                throw new ValidatorException("Failed to decompress mod files");
            BlocksDecoder.Inflate();
            byte[] ExpectedChecksum = new byte[4];
            Reader.Read(ExpectedChecksum, 0, 4);
            Reverse(ExpectedChecksum);
            if (BlocksDecoder.Checksum != ToUInt32(ExpectedChecksum, 0))
                throw new ValidatorException("Failed to decompress mod files");
        }
    }
}