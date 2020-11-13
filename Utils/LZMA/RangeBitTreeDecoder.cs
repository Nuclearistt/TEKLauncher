namespace TEKLauncher.Utils.LZMA
{
    internal struct RangeBitTreeDecoder
    {
        internal RangeBitTreeDecoder(int BitLevelsCount) => Decoders = new RangeBitDecoder[1 << (this.BitLevelsCount = BitLevelsCount)];
        private readonly int BitLevelsCount;
        private readonly RangeBitDecoder[] Decoders;
        internal void Initialize()
        {
            for (int Iterator = 1; Iterator < Decoders.Length; Iterator++)
                Decoders[Iterator].Prob = 1024U;
        }
        internal uint Decode(RangeDecoder RangeDecoder)
        {
            uint Temp = 1U;
            for (int BitIndex = BitLevelsCount; BitIndex > 0; BitIndex--)
                Temp = (Temp << 1) + Decoders[Temp].Decode(RangeDecoder);
            return Temp - (1U << BitLevelsCount);
        }
        internal uint ReverseDecode(RangeDecoder RangeDecoder)
        {
            uint Symbol = 0U, Temp = 1U;
            for (int BitIndex = 0; BitIndex < BitLevelsCount; BitIndex++)
            {
                uint Bit = Decoders[Temp].Decode(RangeDecoder);
                Temp <<= 1;
                Temp += Bit;
                Symbol |= Bit << BitIndex;
            }
            return Symbol;
        }
        internal static uint ReverseDecode(RangeBitDecoder[] Decoders, uint StartIndex, int BitLevelsCount, RangeDecoder RangeDecoder)
        {
            uint Symbol = 0U, Temp = 1U;
            for (int BitIndex = 0; BitIndex < BitLevelsCount; BitIndex++)
            {
                uint Bit = Decoders[StartIndex + Temp].Decode(RangeDecoder);
                Temp <<= 1;
                Temp += Bit;
                Symbol |= Bit << BitIndex;
            }
            return Symbol;
        }
    }
}