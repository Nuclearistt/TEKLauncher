namespace TEKLauncher.Utils.LZMA;

ref struct LZWindow
{
    int _outputOffset = 0;
    int _pos = 0;
    int _streamPos = 0;
    readonly Span<byte> _buffer;
    readonly Span<byte> _stream;
    public LZWindow(Span<byte> buffer, Span<byte> output)
    {
        _buffer = buffer;
        _stream = output;
    }
    public int TrainSize { get; private set; } = 0;
    public void CopyBlock(int offset, int length)
    {
        for (int pos = _pos - offset - 1; length > 0; length--)
        {
            if (pos >= _buffer.Length)
                pos = 0;
            _buffer[_pos++] = _buffer[pos++];
            if (_pos >= _buffer.Length)
                Flush();
        }
    }
    public void Flush()
    {
        int size = _pos - _streamPos;
        if (size == 0)
            return;
        _buffer.Slice(_streamPos, size).CopyTo(_stream.Slice(_outputOffset, size));
        _outputOffset += size;
        if (_pos >= _buffer.Length)
            _pos = 0;
        _streamPos = _pos;
    }
    public void PutByte(byte value)
    {
        _buffer[_pos++] = value;
        if (_pos >= _buffer.Length)
            Flush();
    }
    public void Train(ReadOnlySpan<byte> trainData)
    {
        int size = TrainSize = Math.Min(trainData.Length, _buffer.Length);
        int offset = trainData.Length - size;
        while (size > 0)
        {
            int bytesToRead = _buffer.Length - _pos;
            if (bytesToRead > size)
                bytesToRead = size;
            trainData.Slice(offset, bytesToRead).CopyTo(_buffer.Slice(_pos, bytesToRead));
            offset += bytesToRead;
            size -= bytesToRead;
            _pos += bytesToRead;
            _streamPos += bytesToRead;
            if (_pos == _buffer.Length)
                _streamPos = _pos = 0;
        }
    }
    public byte GetByte(int offset) => _buffer[_pos - offset - 1];
}