namespace TEKLauncher.Utils.LZMA;

struct LenDecoder
{
    RangeBitDecoder _decoderA = new();
    RangeBitDecoder _decoderB = new();
    readonly RangeBitTreeDecoder _highDecoder = new(8);
    readonly RangeBitTreeDecoder[] _lowDecoder = { new(3), new(3), new(3), new(3), new(3), new(3), new(3), new(3), new(3), new(3), new(3), new(3), new(3), new(3), new(3), new(3) };
    readonly RangeBitTreeDecoder[] _midDecoder = { new(3), new(3), new(3), new(3), new(3), new(3), new(3), new(3), new(3), new(3), new(3), new(3), new(3), new(3), new(3), new(3) };
    public LenDecoder() { }
    public void Initialize(int numPosStates)
    {
        _decoderB.State = _decoderA.State = 1024;
        for (int i = 0; i < numPosStates; i++)
        {
            _lowDecoder[i].Initialize();
            _midDecoder[i].Initialize();
        }
        _highDecoder.Initialize();
    }
    public int Decode(ref RangeDecoder RangeDecoder, int posState)
    {
        if (_decoderA.Decode(ref RangeDecoder) == 0)
            return _lowDecoder[posState].Decode(ref RangeDecoder);
        else
            return 8 + (_decoderB.Decode(ref RangeDecoder) == 0 ? _midDecoder[posState].Decode(ref RangeDecoder) : (8 + _highDecoder.Decode(ref RangeDecoder)));
    }
}