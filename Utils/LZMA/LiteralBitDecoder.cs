namespace TEKLauncher.Utils.LZMA
{
    internal struct LiteralBitDecoder
    {
        internal RangeBitDecoder[] Decoders;
        internal byte Decode(RangeDecoder RangeDecoder)
        {
            uint Symbol = 1U;
            while (Symbol < 256U)
                Symbol = (Symbol << 1) | Decoders[Symbol].Decode(RangeDecoder);
            return (byte)Symbol;
        }
        internal byte Decode(RangeDecoder RangeDecoder, byte MatchByte)
        {
            uint Match = MatchByte, Symbol = 1U;
            while (Symbol < 256U)
            {
                uint MatchBit = (Match >> 7) & 1U, Bit = Decoders[((1U + MatchBit) << 8) + Symbol].Decode(RangeDecoder);
                Match <<= 1;
                Symbol = (Symbol << 1) | Bit;
                if (MatchBit != Bit)
                {
                    while (Symbol < 256U)
                        Symbol = (Symbol << 1) | Decoders[Symbol].Decode(RangeDecoder);
                    break;
                }
            }
            return (byte)Symbol;
        }
    }
}