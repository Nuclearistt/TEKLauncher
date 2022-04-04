namespace TEKLauncher.Utils.LZMA;

/// <summary>LZMA decoder implementation optimized for decompressing Steam chunks and refactored to use latest .NET/C# features.</summary>
/// <remarks>Credits to Igor Pavlov for the LZMA algorithm and its original C# implementation.</remarks>
struct Decoder
{
    LenDecoder _lenDecoder = new();
    LenDecoder _repLenDecoder = new();
    LitDecoder _litDecoder = new();
    readonly RangeBitTreeDecoder _posAlignDecoder = new(4);
    readonly RangeBitTreeDecoder[] _posSlotDecoders = { new(6), new(6), new(6), new(6) };
    public Decoder() { }
    /// <summary>Decodes a chunk of data, or optionally patches it upon existing chunk.</summary>
    /// <param name="input">Compressed chunk (or patch) data.</param>
    /// <param name="output">Buffer that will receive decompressed chunk data.</param>
    /// <param name="trainData">Old chunk data to train upon for applying patch, pass <see langword="default"/> if no patching is needed.</param>
    /// <returns><see langword="true"/> if decoding succeeds; otherwise, <see langword="false"/>.</returns>
    public bool Decode(ReadOnlySpan<byte> input, Span<byte> output, ReadOnlySpan<byte> trainData)
    {
        int dictSize = BitConverter.ToInt32(input.Slice(8, 4));
        int dictSizeCheck = dictSize;
        if (dictSizeCheck == 0)
            dictSizeCheck++;
        var window = new LZWindow(stackalloc byte[Math.Max(dictSize, 4096)], output);
        int quotient = input[7] / 9;
        _litDecoder.Initialize(quotient % 5, input[7] % 9);
        int numPosStates = 1 << (quotient / 5);
        int posStateMask = numPosStates - 1;
        _lenDecoder.Initialize(numPosStates);
        _repLenDecoder.Initialize(numPosStates);
        if (trainData.Length > 0)
            window.Train(trainData);
        var rangeDecoder = new RangeDecoder(input[12..]);
        Span<RangeBitDecoder> matchDecoders = stackalloc RangeBitDecoder[192];
        Span<RangeBitDecoder> posDecoders = stackalloc RangeBitDecoder[114];
        Span<RangeBitDecoder> repDecoders = stackalloc RangeBitDecoder[12];
        Span<RangeBitDecoder> repG0Decoders = stackalloc RangeBitDecoder[12];
        Span<RangeBitDecoder> repG1Decoders = stackalloc RangeBitDecoder[12];
        Span<RangeBitDecoder> repG2Decoders = stackalloc RangeBitDecoder[12];
        Span<RangeBitDecoder> rep0LongDecoders = stackalloc RangeBitDecoder[192];
        for (int i = 0; i < 12; i++)
        {
            repG2Decoders[i].State = repG1Decoders[i].State = repG0Decoders[i].State = repDecoders[i].State = 1024;
            for (int j = 0; j < numPosStates; j++)
            {
                int index = (i << 4) + j;
                rep0LongDecoders[index].State = matchDecoders[index].State = 1024;
            }
        }
        for (int i = 0; i < 114; i++)
            posDecoders[i].State = 1024;
        _posAlignDecoder.Initialize();
        foreach (var decoder in _posSlotDecoders)
            decoder.Initialize();
        int rep0 = 0, rep1 = 0, rep2 = 0, rep3 = 0;
        int pos = 0;
        int state = 0;
        if (trainData.Length == 0)
        {
            if (matchDecoders[0].Decode(ref rangeDecoder) != 0)
                return false;
            if (state < 4)
                state = 0;
            else
                state -= state < 10 ? 3 : 6;
            window.PutByte(_litDecoder.Decode(ref rangeDecoder, 0, 0));
            pos++;
        }
        while (pos < output.Length)
        {
            int posState = pos & posStateMask;
            if (matchDecoders[(state << 4) + posState].Decode(ref rangeDecoder) == 0)
            {
                byte prevByte = window.GetByte(0);
                window.PutByte(state < 7 ? _litDecoder.Decode(ref rangeDecoder, pos, prevByte) : _litDecoder.Decode(ref rangeDecoder, pos, prevByte, window.GetByte(rep0)));
                if (state < 4)
                    state = 0;
                else
                    state -= state < 10 ? 3 : 6;
                pos++;
                continue;
            }
            int len;
            if (repDecoders[state].Decode(ref rangeDecoder) == 0)
            {
                rep3 = rep2;
                rep2 = rep1;
                rep1 = rep0;
                len = _lenDecoder.Decode(ref rangeDecoder, posState) + 2;
                state = state < 7 ? 7 : 10;
                int posSlot = _posSlotDecoders[len < 6 ? len - 2 : 3].Decode(ref rangeDecoder);
                if (posSlot > 3)
                {
                    int numDirectBits = (posSlot >> 1) - 1;
                    rep0 = (2 | (posSlot & 1)) << numDirectBits;
                    if (posSlot < 14)
                    {
                        int index = 1;
                        int indexOffset = rep0 - posSlot - 1;
                        int symbol = 0;
                        for (int i = 0; i < numDirectBits; i++)
                        {
                            int bit = posDecoders[indexOffset + index].Decode(ref rangeDecoder);
                            index <<= 1;
                            index += bit;
                            symbol |= bit << i;
                        }
                        rep0 += symbol;
                    }
                    else
                        rep0 += (rangeDecoder.Decode(numDirectBits - 4) << 4) + _posAlignDecoder.ReverseDecode(ref rangeDecoder);
                }
                else
                    rep0 = posSlot;
            }
            else
            {
                if (repG0Decoders[state].Decode(ref rangeDecoder) == 0)
                {
                    if (rep0LongDecoders[(state << 4) + posState].Decode(ref rangeDecoder) == 0)
                    {
                        state = state < 7 ? 9 : 11;
                        window.PutByte(window.GetByte(rep0));
                        pos++;
                        continue;
                    }
                }
                else
                {
                    int dist;
                    if (repG1Decoders[state].Decode(ref rangeDecoder) == 0)
                        dist = rep1;
                    else
                    {
                        if (repG2Decoders[state].Decode(ref rangeDecoder) == 0)
                            dist = rep2;
                        else
                        {
                            dist = rep3;
                            rep3 = rep2;
                        }
                        rep2 = rep1;
                    }
                    rep1 = rep0;
                    rep0 = dist;
                }
                len = _repLenDecoder.Decode(ref rangeDecoder, posState) + 2;
                state = state < 7 ? 8 : 11;
            }
            if (rep0 >= window.TrainSize + pos || rep0 >= dictSizeCheck)
            {
                if (rep0 == int.MaxValue)
                    break;
                else
                    return false;
            }
            window.CopyBlock(rep0, len);
            pos += len;
        }
        window.Flush();
        return true;
    }
}