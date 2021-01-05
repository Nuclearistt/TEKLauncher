using System.IO;
using TEKLauncher.SteamInterop.Network;
using static TEKLauncher.Data.LocalizationManager;

namespace TEKLauncher.Utils.Zlib
{
    internal class InflateBlocksDecoder
    {
        internal InflateBlocksDecoder(ZlibDecompressor Decompressor, FileStream Writer)
        {
            this.Writer = Writer;
            CodesDecoder = new InflateCodesDecoder(this);
            this.Decompressor = Decompressor;
        }
        private int TreeIndex;
        internal int BitBuffer, BitBufferShift, ReadOffset, WriteOffset;
        internal uint Checksum = 1U;
        private readonly int[] Trees = new int[4320];
        private readonly FileStream Writer;
        private readonly InflateCodesDecoder CodesDecoder;
        private readonly InflateTreesDecoder TreesDecoder = new InflateTreesDecoder();
        internal readonly byte[] Window = new byte[32768];
        internal readonly ZlibDecompressor Decompressor;
        private static readonly int[] Border = new[] { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };
        internal static readonly int[] InflationMask = new[]
        {
            0x00000000, 0x00000001, 0x00000003, 0x00000007, 0x0000000F, 0x0000001F, 0x0000003F, 0x0000007F,
            0x000000FF, 0x000001FF, 0x000003FF, 0x000007FF, 0x00000FFF, 0x00001FFF, 0x00003FFF, 0x00007FFF, 0x0000FFFF
        };
        internal void Inflate()
        {
            bool LastBlock = false;
            int RemainingBytes = WriteOffset < ReadOffset ? ReadOffset - WriteOffset - 1 : 32768 - WriteOffset;
            while (!LastBlock)
            {
                BitBuffer |= ReadByte() << BitBufferShift;
                BitBufferShift += 8;
                int Flags = BitBuffer & 7, Mode = Flags >> 1;
                LastBlock = (Flags & 1) != 0;
                if (Mode > 2)
                    throw new ValidatorException(LocString(LocCode.ModDecompressFailed));
                if (Mode == 0)
                {
                    BitBuffer >>= 3;
                    BitBufferShift -= 3;
                    Flags = BitBufferShift & 7;
                    BitBuffer >>= Flags;
                    BitBufferShift -= Flags;
                    while (BitBufferShift < 32)
                    {
                        BitBuffer |= ReadByte() << BitBufferShift;
                        BitBufferShift += 8;
                    }
                    int BytesLeft = BitBuffer & 0xFFFF;
                    if ((((~BitBuffer) >> 16) & 0xFFFF) != BytesLeft)
                        throw new ValidatorException(LocString(LocCode.ModDecompressFailed));
                    BitBufferShift = BitBuffer = 0;
                    while (BytesLeft != 0)
                    {
                        if (RemainingBytes == 0)
                        {
                            if (WriteOffset == 32768 && ReadOffset != 0)
                            {
                                WriteOffset = 0;
                                RemainingBytes = WriteOffset < ReadOffset ? ReadOffset - WriteOffset - 1 : 32768 - WriteOffset;
                            }
                            if (RemainingBytes == 0)
                            {
                                Flush();
                                RemainingBytes = WriteOffset < ReadOffset ? ReadOffset - WriteOffset - 1 : 32768 - WriteOffset;
                                if (WriteOffset == 32768 && ReadOffset != 0)
                                {
                                    WriteOffset = 0;
                                    RemainingBytes = WriteOffset < ReadOffset ? ReadOffset - WriteOffset - 1 : 32768 - WriteOffset;
                                }
                            }
                        }
                        int CopyLength = BytesLeft;
                        if (CopyLength > Decompressor.InputAvailableSize)
                            CopyLength = Decompressor.InputAvailableSize;
                        if (CopyLength > RemainingBytes)
                            CopyLength = RemainingBytes;
                        Decompressor.Reader.Read(Window, WriteOffset, CopyLength);
                        Decompressor.InputAvailableSize -= CopyLength;
                        WriteOffset += CopyLength;
                        BytesLeft -= CopyLength;
                        RemainingBytes -= CopyLength;
                    }
                }
                else
                {
                    if (Mode == 1)
                        CodesDecoder.Initialize();
                    BitBuffer >>= 3;
                    BitBufferShift -= 3;
                    if (Mode == 2)
                    {
                        while (BitBufferShift < 14)
                        {
                            BitBuffer |= ReadByte() << BitBufferShift;
                            BitBufferShift += 8;
                        }
                        int TableLengths = BitBuffer & 0x3FFF, LTreeLength = TableLengths & 0x1F, DTreeLength = (TableLengths >> 5) & 0x1F;
                        if (LTreeLength > 29 || DTreeLength > 29)
                            throw new ValidatorException(LocString(LocCode.ModDecompressFailed));
                        int[] BitLengths = new int[258 + LTreeLength + DTreeLength];
                        BitBuffer >>= 14;
                        BitBufferShift -= 14;
                        int Index = 0, IndexLimit = 4 + (TableLengths >> 10);
                        while (Index < IndexLimit)
                        {
                            if (BitBufferShift < 3)
                            {
                                BitBuffer |= ReadByte() << BitBufferShift;
                                BitBufferShift += 8;
                            }
                            BitLengths[Border[Index++]] = BitBuffer & 7;
                            BitBuffer >>= 3;
                            BitBufferShift -= 3;
                        }
                        while (Index < 19)
                            BitLengths[Border[Index++]] = 0;
                        TreesDecoder.InflateBitTrees(BitLengths, Trees, ref TreeIndex, out int BitsPerBranch);
                        Index = 0;
                        while (Index < BitLengths.Length)
                        {
                            while (BitBufferShift < BitsPerBranch)
                            {
                                BitBuffer |= ReadByte() << BitBufferShift;
                                BitBufferShift += 8;
                            }
                            int Shift = Trees[(TreeIndex + (BitBuffer & InflationMask[BitsPerBranch])) * 3 + 1], BitLength = Trees[(TreeIndex + (BitBuffer & InflationMask[Shift])) * 3 + 2];
                            if (BitLength < 16)
                            {
                                BitBuffer >>= Shift;
                                BitBufferShift -= Shift;
                                BitLengths[Index++] = BitLength;
                            }
                            else
                            {
                                int Offset = BitLength == 18 ? 7 : BitLength - 14;
                                while (BitBufferShift < Shift + Offset)
                                {
                                    BitBuffer |= ReadByte() << BitBufferShift;
                                    BitBufferShift += 8;
                                }
                                BitBuffer >>= Shift;
                                BitBufferShift -= Shift;
                                int RepeatCount = (BitLength == 18 ? 11 : 3) + (BitBuffer & InflationMask[Offset]);
                                BitBuffer >>= Offset;
                                BitBufferShift -= Offset;
                                if (Index + RepeatCount > BitLengths.Length || BitLength == 16 && Index < 1)
                                    throw new ValidatorException(LocString(LocCode.ModDecompressFailed));
                                BitLength = BitLength == 16 ? BitLengths[Index - 1] : 0;
                                for (; RepeatCount > 0; RepeatCount--)
                                    BitLengths[Index++] = BitLength;
                            }
                        }
                        TreeIndex = -1;
                        TreesDecoder.InflateDynamicTrees(1 + DTreeLength, 257 + LTreeLength, BitLengths, Trees, out int DBitsPerBranch, out int LBitsPerBranch, out int DTreeIndex, out int LTreeIndex);
                        CodesDecoder.Initialize(DBitsPerBranch, LBitsPerBranch, DTreeIndex, LTreeIndex, Trees);
                    }
                    CodesDecoder.Decode();
                    RemainingBytes = WriteOffset < ReadOffset ? ReadOffset - WriteOffset - 1 : 32768 - WriteOffset;
                }
            }
            Flush();
        }
        internal void Flush()
        {
            int ReadSize = (ReadOffset <= WriteOffset ? WriteOffset : 32768) - ReadOffset;
            for (int Iterator = 0; Iterator < 2; Iterator++)
            {
                if (Iterator == 1)
                    ReadSize = WriteOffset - ReadOffset;
                if (ReadSize == 0)
                    break;
                if (ReadSize > Decompressor.OutputAvailableSize)
                    ReadSize = Decompressor.OutputAvailableSize;
                int AdlerOffset = ReadOffset, AdlerSize = ReadSize;
                uint A = Checksum & 0xFFFFU, B = (Checksum >> 16) & 0xFFFFU;
                while (AdlerSize > 0)
                {
                    int ChunkSize = AdlerSize < 5552 ? AdlerSize : 5552;
                    AdlerSize -= ChunkSize;
                    for (; ChunkSize > 0; ChunkSize--)
                        B += A += Window[AdlerOffset++];
                    A %= 65521;
                    B %= 65521;
                }
                Checksum = A | (B << 16);
                Writer.Write(Window, ReadOffset, ReadSize);
                Decompressor.OutputAvailableSize -= ReadSize;
                ReadOffset += ReadSize;
                if (ReadOffset == 32768 && Iterator == 0)
                {
                    ReadOffset = 0;
                    if (WriteOffset == 32768)
                        WriteOffset = 0;
                }
                else
                    break;
            }
        }
        internal int ReadByte()
        {
            Decompressor.InputAvailableSize--;
            return Decompressor.Reader.ReadByte();
        }
    }
}