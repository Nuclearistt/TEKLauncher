using System.IO;
using TEKLauncher.SteamInterop.Network;
using TEKLauncher.Utils.LZMA;

namespace TEKLauncher.Utils
{
    internal class VZip
    {
        internal VZip(int ThreadsCount)
        {
            Decoders = new Decoder[ThreadsCount];
            for (int Iterator = 0; Iterator < ThreadsCount; Iterator++)
                Decoders[Iterator] = new Decoder();
        }
        private readonly Decoder[] Decoders;
        internal byte[] Decompress(byte[] Input, int Index)
        {
            byte[] DecompressedData;
            using (MemoryStream Stream = new MemoryStream(Input))
            using (BinaryReader Reader = new BinaryReader(Stream))
            {
                if (Reader.ReadUInt16() != 0x5A56U)
                    throw new ValidatorException("Failed to decompress chunk");
                if (Reader.ReadChar() != 'a')
                    throw new ValidatorException("Failed to decompress chunk");
                Stream.Position += 4L;
                byte[] Properties = new byte[5];
                Stream.Read(Properties, 0, 5);
                Decoders[Index].SetProperties(Properties);
                long ReadPosition = Stream.Position;
                Stream.Position += Stream.Length - 18L;
                uint UncompressedSize = Reader.ReadUInt32();
                if (Reader.ReadUInt16() != 0x767AU)
                    throw new ValidatorException("Failed to decompress chunk");
                Stream.Position = ReadPosition;
                using (MemoryStream DecoderStream = new MemoryStream(DecompressedData = new byte[UncompressedSize]))
                    Decoders[Index].Decode(Stream, DecoderStream, UncompressedSize);
            }
            return DecompressedData;
        }
    }
}