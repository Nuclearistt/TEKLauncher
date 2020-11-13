namespace TEKLauncher.Utils.LZMA
{
    internal class LiteralDecoder
    {
        private int PositionBitsCount, PreviousBitsCount;
        private uint PositionMask;
        private LiteralBitDecoder[] Decoders;
        private uint GetState(uint Position, byte PreviousByte) => ((Position & PositionMask) << PreviousBitsCount) + ((uint)PreviousByte >> (8 - PreviousBitsCount));
        internal void Create(int PositionBitsCount, int PreviousBitsCount)
        {
            if (Decoders is null || this.PositionBitsCount != PositionBitsCount || this.PreviousBitsCount != PreviousBitsCount)
            {
                PositionMask = (1U << (this.PositionBitsCount = PositionBitsCount)) - 1U;
                uint StatesCount = 1U << ((this.PreviousBitsCount = PreviousBitsCount) + PositionBitsCount);
                Decoders = new LiteralBitDecoder[StatesCount];
                for (uint Iterator = 0U; Iterator < StatesCount; Iterator++)
                    Decoders[Iterator].Decoders = new RangeBitDecoder[768];
            }
        }
        internal void Initialize()
        {
            for (int Iterator = 0; Iterator < Decoders.Length; Iterator++)
                for (int Iterator1 = 0; Iterator1 < 768; Iterator1++)
                    Decoders[Iterator].Decoders[Iterator1].Prob = 1024U;
        }
        internal byte Decode(RangeDecoder RangeDecoder, uint Position, byte PreviousByte) => Decoders[GetState(Position, PreviousByte)].Decode(RangeDecoder);
        internal byte Decode(RangeDecoder RangeDecoder, uint Position, byte PreviousByte, byte MatchByte) => Decoders[GetState(Position, PreviousByte)].Decode(RangeDecoder, MatchByte);
    }
}