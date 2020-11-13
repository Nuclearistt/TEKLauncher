namespace TEKLauncher.Utils.LZMA
{
    internal struct RangeBitDecoder
    {
        internal uint Prob;
        internal uint Decode(RangeDecoder RangeDecoder)
        {
            uint NewBound = (RangeDecoder.Range >> 11) * Prob;
            if (RangeDecoder.Code < NewBound)
            {
                RangeDecoder.Range = NewBound;
                Prob += (2048U - Prob) >> 5;
                if (RangeDecoder.Range < 16777216U)
                {
                    RangeDecoder.Code = (RangeDecoder.Code << 8) | (uint)RangeDecoder.Stream.ReadByte();
                    RangeDecoder.Range <<= 8;
                }
                return 0U;
            }
            else
            {
                RangeDecoder.Code -= NewBound;
                RangeDecoder.Range -= NewBound;
                Prob -= Prob >> 5;
                if (RangeDecoder.Range < 16777216U)
                {
                    RangeDecoder.Code = (RangeDecoder.Code << 8) | (uint)RangeDecoder.Stream.ReadByte();
                    RangeDecoder.Range <<= 8;
                }
                return 1U;
            }
        }
    }
}