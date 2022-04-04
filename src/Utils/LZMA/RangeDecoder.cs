namespace TEKLauncher.Utils.LZMA;

ref struct RangeDecoder
{
    int _byteIndex = 0;
    public uint Code = 0;
    public uint Range = uint.MaxValue;
    readonly ReadOnlySpan<byte> _data;
    public RangeDecoder(ReadOnlySpan<byte> data)
    {
        _data = data;
        //Unwrapped loop
        Code <<= 8;
        Code |= NextByte();
        Code <<= 8;
        Code |= NextByte();
        Code <<= 8;
        Code |= NextByte();
        Code <<= 8;
        Code |= NextByte();
        Code <<= 8;
        Code |= NextByte();
    }
    public int Decode(int numTotalBits)
    {
        int result = 0;
        for (int i = 0; i < numTotalBits; i++)
        {
            Range >>= 1;
            uint temp = (Code - Range) >> 31;
            Code -= Range & (temp - 1);
            result <<= 1;
            result |= 1 - (int)temp;
            if (Range < 16777216)
            {
                Code <<= 8;
                Code |= NextByte();
                Range <<= 8;
            }
        }
        return result;
    }
    public uint NextByte() => _byteIndex < _data.Length ? _data[_byteIndex++] : uint.MaxValue;
}