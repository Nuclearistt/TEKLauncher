using System.IO;

namespace TEKLauncher.Utils.LZMA
{
    internal class RangeDecoder
    {
        internal uint Code, Range;
        internal Stream Stream;
        internal void Initialize(Stream Stream)
        {
            Code = 0U;
            Range = uint.MaxValue;
            this.Stream = Stream;
            for (int Iterator = 0; Iterator < 5; Iterator++)
                Code = (Code << 8) | (uint)Stream.ReadByte();
        }
        internal uint Decode(int TotalBitsCount)
        {
            uint Result = 0U;
            for (; TotalBitsCount > 0; TotalBitsCount--)
            {
                uint Temp = (Code - (Range >>= 1)) >> 31;
                Code -= Range & (Temp - 1U);
                Result = (Result << 1) | (1U - Temp);
                if (Range < 16777216U)
                {
                    Code = (Code << 8) | (uint)Stream.ReadByte();
                    Range <<= 8;
                }
            }
            return Result;
        }
    }
}