namespace TEKLauncher.Utils.LZMA;

struct RangeBitDecoder
{
    public uint State;
    public int Decode(ref RangeDecoder rangeDecoder)
    {
        uint newBound = (rangeDecoder.Range >> 11) * State;
        if (rangeDecoder.Code < newBound)
        {
            rangeDecoder.Range = newBound;
            State += (2048 - State) >> 5;
            if (rangeDecoder.Range < 16777216)
            {
                rangeDecoder.Code <<= 8;
                rangeDecoder.Code |= rangeDecoder.NextByte();
                rangeDecoder.Range <<= 8;
            }
            return 0;
        }
        else
        {
            rangeDecoder.Code -= newBound;
            rangeDecoder.Range -= newBound;
            State -= State >> 5;
            if (rangeDecoder.Range < 16777216)
            {
                rangeDecoder.Code <<= 8;
                rangeDecoder.Code |= rangeDecoder.NextByte();
                rangeDecoder.Range <<= 8;
            }
            return 1;
        }
    }
}