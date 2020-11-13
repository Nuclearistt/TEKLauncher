using System.IO;
using TEKLauncher.SteamInterop.Network;
using static System.Math;

namespace TEKLauncher.Utils.LZMA
{
    internal class Decoder
    {
        private uint DictionarySize = uint.MaxValue, DictionarySizeCheck, PositionStateMask;
        private readonly LengthDecoder LengthDecoder = new LengthDecoder(), RepLengthDecoder = new LengthDecoder();
        private readonly LiteralDecoder LiteralDecoder = new LiteralDecoder();
        private readonly LZWindow Window = new LZWindow();
        private readonly RangeBitDecoder[] MatchDecoders = new RangeBitDecoder[192],
            PositionDecoders = new RangeBitDecoder[114],
            RepDecoders = new RangeBitDecoder[12],
            Rep0LongDecoders = new RangeBitDecoder[192],
            RepG0Decoders = new RangeBitDecoder[12],
            RepG1Decoders = new RangeBitDecoder[12],
            RepG2Decoders = new RangeBitDecoder[12];
        private readonly RangeBitTreeDecoder PositionAlignDecoder = new RangeBitTreeDecoder(4);
        private readonly RangeBitTreeDecoder[] PositionSlotDecoder = new[]
        {
            new RangeBitTreeDecoder(6),
            new RangeBitTreeDecoder(6),
            new RangeBitTreeDecoder(6),
            new RangeBitTreeDecoder(6)
        };
        private readonly RangeDecoder RangeDecoder = new RangeDecoder();
        internal void Decode(Stream InputStream, Stream OutputStream, uint UncompressedSize)
        {
            RangeDecoder.Initialize(InputStream);
            Window.Initialize(OutputStream);
            LengthDecoder.Initialize();
            RepLengthDecoder.Initialize();
            LiteralDecoder.Initialize();
            for (uint Iterator = 0U; Iterator < 12U; Iterator++)
            {
                RepG2Decoders[Iterator].Prob = RepG1Decoders[Iterator].Prob = RepG0Decoders[Iterator].Prob = RepDecoders[Iterator].Prob = 1024U;
                for (uint Iterator1 = 0U; Iterator1 <= PositionStateMask; Iterator1++)
                {
                    uint Index = (Iterator << 4) + Iterator1;
                    Rep0LongDecoders[Index].Prob = MatchDecoders[Index].Prob = 1024U;
                }
            }
            for (int Iterator = 0; Iterator < 114; Iterator++)
                PositionDecoders[Iterator].Prob = 1024U;
            PositionAlignDecoder.Initialize();
            foreach (RangeBitTreeDecoder BitTreeDecoder in PositionSlotDecoder)
                BitTreeDecoder.Initialize();
            uint Rep0 = 0U, Rep1 = 0U, Rep2 = 0U, Rep3 = 0U;
            State State = new State();
            if (MatchDecoders[0].Decode(RangeDecoder) != 0U)
                throw new ValidatorException("Failed to decompress chunk");
            State.UpdateChar();
            Window.PutByte(LiteralDecoder.Decode(RangeDecoder, 0U, 0));
            for (uint Position = 1U; Position < UncompressedSize;)
            {
                uint PositionState = Position & PositionStateMask;
                if (MatchDecoders[(State.Index << 4) + PositionState].Decode(RangeDecoder) == 0U)
                {
                    byte PreviousByte = Window.GetByte(0U);
                    Window.PutByte(State.IsCharState ? LiteralDecoder.Decode(RangeDecoder, Position, PreviousByte) : LiteralDecoder.Decode(RangeDecoder, Position, PreviousByte, Window.GetByte(Rep0)));
                    State.UpdateChar();
                    Position++;
                }
                else
                {
                    uint Length;
                    if (RepDecoders[State.Index].Decode(RangeDecoder) == 1U)
                    {
                        if (RepG0Decoders[State.Index].Decode(RangeDecoder) == 0U)
                        {
                            if (Rep0LongDecoders[(State.Index << 4) + PositionState].Decode(RangeDecoder) == 0U)
                            {
                                State.UpdateShortRep();
                                Window.PutByte(Window.GetByte(Rep0));
                                Position++;
                                continue;
                            }
                        }
                        else
                        {
                            uint Distance;
                            if (RepG1Decoders[State.Index].Decode(RangeDecoder) == 0U)
                                Distance = Rep1;
                            else
                            {
                                if (RepG2Decoders[State.Index].Decode(RangeDecoder) == 0U)
                                    Distance = Rep2;
                                else
                                {
                                    Distance = Rep3;
                                    Rep3 = Rep2;
                                }
                                Rep2 = Rep1;
                            }
                            Rep1 = Rep0;
                            Rep0 = Distance;
                        }
                        Length = RepLengthDecoder.Decode(RangeDecoder, PositionState) + 2U;
                        State.UpdateRep();
                    }
                    else
                    {
                        Rep3 = Rep2;
                        Rep2 = Rep1;
                        Rep1 = Rep0;
                        Length = LengthDecoder.Decode(RangeDecoder, PositionState) + 2U;
                        State.UpdateMatch();
                        uint PositionSlot = PositionSlotDecoder[Length - 2U < 4U ? Length - 2U : 3U].Decode(RangeDecoder);
                        if (PositionSlot > 3U)
                        {
                            int DirectBitsCount = (int)((PositionSlot >> 1) - 1U);
                            Rep0 = (2U | (PositionSlot & 1U)) << DirectBitsCount;
                            Rep0 += (PositionSlot < 14U) ? RangeBitTreeDecoder.ReverseDecode(PositionDecoders, Rep0 - PositionSlot - 1U, DirectBitsCount, RangeDecoder) : ((RangeDecoder.Decode(DirectBitsCount - 4) << 4) + PositionAlignDecoder.ReverseDecode(RangeDecoder));
                        }
                        else
                            Rep0 = PositionSlot;
                    }
                    if (Rep0 >= Position || Rep0 >= DictionarySizeCheck)
                    {
                        if (Rep0 == uint.MaxValue)
                            break;
                        else
                            throw new ValidatorException("Failed to decompress chunk");
                    }
                    Window.CopyBlock(Rep0, Length);
                    Position += Length;
                }
            }
            Window.Flush();
        }
        internal void SetProperties(byte[] Properties)
        {
            uint DictionarySize = 0;
            for (int Iterator = 0; Iterator < 4; Iterator++)
                DictionarySize += ((uint)Properties[1 + Iterator]) << (Iterator * 8);
            if (this.DictionarySize != DictionarySize)
                Window.Create(Max(DictionarySizeCheck = Max(this.DictionarySize = DictionarySize, 1), 4096));
            int LC = Properties[0] % 9, Remainder = Properties[0] / 9, LP = Remainder % 5, PB = Remainder / 5;
            if (LC > 8 || LP > 8 || PB > 4)
                throw new ValidatorException("Failed to decompress chunk");
            LiteralDecoder.Create(LP, LC);
            uint PositionStatesCount = 1U << PB;
            PositionStateMask = PositionStatesCount - 1U;
            LengthDecoder.Create(PositionStatesCount);
            RepLengthDecoder.Create(PositionStatesCount);
        }
    }
}