namespace TEKLauncher.Utils.LZMA
{
    internal class LengthDecoder
    {
        private uint PositionStatesCount;
        private RangeBitDecoder ChoiceA = new RangeBitDecoder(), ChoiceB = new RangeBitDecoder();
        private readonly RangeBitTreeDecoder HighDecoder = new RangeBitTreeDecoder(8);
        private readonly RangeBitTreeDecoder[] LowDecoder = new RangeBitTreeDecoder[16], MidDecoder = new RangeBitTreeDecoder[16];
        internal void Create(uint PositionStatesCount)
        {
            for (uint PositionState = this.PositionStatesCount; PositionState < PositionStatesCount; PositionState++)
            {
                LowDecoder[PositionState] = new RangeBitTreeDecoder(3);
                MidDecoder[PositionState] = new RangeBitTreeDecoder(3);
            }
            this.PositionStatesCount = PositionStatesCount;
        }
        internal void Initialize()
        {
            ChoiceB.Prob = ChoiceA.Prob = 1024U;
            for (uint PositionState = 0U; PositionState < PositionStatesCount; PositionState++)
            {
                LowDecoder[PositionState].Initialize();
                MidDecoder[PositionState].Initialize();
            }
            HighDecoder.Initialize();
        }
        internal uint Decode(RangeDecoder RangeDecoder, uint PositionState) => ChoiceA.Decode(RangeDecoder) == 0U ? LowDecoder[PositionState].Decode(RangeDecoder) : 8U + (ChoiceB.Decode(RangeDecoder) == 0U ? MidDecoder[PositionState].Decode(RangeDecoder) : (8U + HighDecoder.Decode(RangeDecoder)));
    }
}