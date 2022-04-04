namespace TEKLauncher.Utils.LZMA;

struct RangeBitTreeDecoder
{
    readonly int _numBitLevels;
    readonly RangeBitDecoder[] _decoders;
    public RangeBitTreeDecoder(int numBitLevels)
    {
        _numBitLevels = numBitLevels;
        _decoders = new RangeBitDecoder[1 << numBitLevels];
    }
    public void Initialize()
    {
        for (int i = 0; i < _decoders.Length; i++)
            _decoders[i].State = 1024;
    }
    public int Decode(ref RangeDecoder rangeDecoder)
    {
        int temp = 1;
        for (int i = _numBitLevels; i > 0; i--)
            temp = (temp << 1) + _decoders[temp].Decode(ref rangeDecoder);
        return temp - _decoders.Length;
    }
    public int ReverseDecode(ref RangeDecoder rangeDecoder)
    {
        int symbol = 0;
        for (int i = 0, index = 1; i < _numBitLevels; i++)
        {
            int bit = _decoders[index].Decode(ref rangeDecoder);
            index <<= 1;
            index += bit;
            symbol |= bit << i;
        }
        return symbol;
    }
}