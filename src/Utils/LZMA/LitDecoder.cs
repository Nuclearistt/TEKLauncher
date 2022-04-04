namespace TEKLauncher.Utils.LZMA;

struct LitDecoder
{
    int _numPrevBits = 0;
    int _posMask = 0;
    RangeBitDecoder[,] _decoders = null!;
    public LitDecoder() { }
    public void Initialize(int numPosBits, int numPrevBits)
    {
        int posMask = (1 << numPosBits) - 1;
        if (_posMask != posMask || _numPrevBits != numPrevBits)
        {
            _numPrevBits = numPrevBits;
            _posMask = posMask;
            int numStates = 1 << (numPrevBits + numPosBits);
            _decoders = new RangeBitDecoder[numStates, 768];
        }
        for (int i = 0; i < _decoders.GetLength(0); i++)
            for (int j = 0; j < 768; j++)
                _decoders[i, j].State = 1024;
    }
    public byte Decode(ref RangeDecoder randeDecoder, int pos, int prevByte)
    {
        int decoderIndex = ((pos & _posMask) << _numPrevBits) + (prevByte >> (8 - _numPrevBits));
        int symbol = 1;
        while (symbol < 256)
            symbol = (symbol << 1) | _decoders[decoderIndex, symbol].Decode(ref randeDecoder);
        return (byte)symbol;
    }
    public byte Decode(ref RangeDecoder rangeDecoder, int pos, int prevByte, int matchByte)
    {
        int decoderIndex = ((pos & _posMask) << _numPrevBits) + (prevByte >> (8 - _numPrevBits));
        int symbol = 1;
        while (symbol < 256)
        {
            int matchBit = (matchByte >> 7) & 1;
            int bit = _decoders[decoderIndex, ((matchBit + 1) << 8) + symbol].Decode(ref rangeDecoder);
            matchByte <<= 1;
            symbol = (symbol << 1) | bit;
            if (matchBit != bit)
            {
                while (symbol < 256)
                    symbol = (symbol << 1) | _decoders[decoderIndex, symbol].Decode(ref rangeDecoder);
                break;
            }
        }
        return (byte)symbol;
    }
}