using TEKLauncher.SteamInterop.Network;
using static System.Array;

namespace TEKLauncher.Utils.Zlib
{
    internal class InflateTreesDecoder
    {
        private int TreesCount;
        private int[] Workspace;
        private readonly int[] BitOffsets = new int[16], LCountTable = new int[16], TableEntry = new int[3], TableStack = new int[15];
        private static readonly int[] CPDExtra = new[] { 0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13 },
            CPDTable = new[] { 1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193, 257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577 },
            CPLExtra = new[] { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0, 112, 112 },
            CPLTable = new[] { 3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31, 35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258, 0, 0 };
        private void InitializeWorkspace(int Size)
        {
            if ((Workspace?.Length ?? 0) < Size)
                Workspace = new int[Size];
            Clear(Workspace, 0, Size);
            Clear(BitOffsets, 0, 16);
            Clear(LCountTable, 0, 16);
            Clear(TableEntry, 0, 3);
            Clear(TableStack, 0, 15);
        }
        private void BuildTree(int[] BitLengths, int LengthIndex, int TreeLength, int Offset, int[] Table, int[] ExtraTable, ref int TreeIndex, ref int BitsPerBranch, int[] Trees)
        {
            for (int Iterator = 0; Iterator < TreeLength; Iterator++)
                LCountTable[BitLengths[LengthIndex + Iterator]]++;
            if (LCountTable[0] == TreeLength)
            {
                BitsPerBranch = 0;
                TreeIndex = -1;
                return;
            }
            int CurrentCodeLength = 1;
            for (; CurrentCodeLength < 16; CurrentCodeLength++)
                if (LCountTable[CurrentCodeLength] != 0)
                    break;
            if (BitsPerBranch < CurrentCodeLength)
                BitsPerBranch = CurrentCodeLength;
            int MaxCodeLength = 15;
            for (; MaxCodeLength > 0; MaxCodeLength--)
                if (LCountTable[MaxCodeLength] != 0)
                    break;
            if (BitsPerBranch > MaxCodeLength)
                BitsPerBranch = MaxCodeLength;
            int LastCountIncrease = 1 << CurrentCodeLength;
            for (int Iterator = CurrentCodeLength; Iterator < MaxCodeLength; Iterator++, LastCountIncrease <<= 1)
                if ((LastCountIncrease -= LCountTable[Iterator]) < 0)
                    throw new ValidatorException("Failed to decompress mod files");
            if ((LastCountIncrease -= LCountTable[MaxCodeLength]) < 0)
                throw new ValidatorException("Failed to decompress mod files");
            LCountTable[MaxCodeLength] += LastCountIncrease;
            int CountsSum = BitOffsets[1] = 0;
            for (int Iterator = 1, BitOffsetIndex = 2, CountIndex = 1; Iterator < MaxCodeLength; Iterator++)
                BitOffsets[BitOffsetIndex++] = CountsSum += LCountTable[CountIndex++];
            for (int Iterator = 0; Iterator < TreeLength; Iterator++)
            {
                int Length = BitLengths[LengthIndex + Iterator];
                if (Length != 0)
                    Workspace[BitOffsets[Length]++] = Iterator;
            }
            TreeLength = BitOffsets[MaxCodeLength];
            TableStack[0] = BitOffsets[0] = 0;
            int BitsDecoded = -BitsPerBranch, CurrentCode = 0, EntriesCount = 0, TableIndex = 0, TableLevel = -1, WorkspaceIndex = 0;
            for (; CurrentCodeLength <= MaxCodeLength; CurrentCodeLength++)
                for (int Count = LCountTable[CurrentCodeLength]; Count > 0; Count--)
                {
                    while (CurrentCodeLength > BitsDecoded + BitsPerBranch)
                    {
                        if ((EntriesCount = MaxCodeLength - (BitsDecoded += BitsPerBranch)) > BitsPerBranch)
                            EntriesCount = BitsPerBranch;
                        int EntryIndex = CurrentCodeLength - BitsDecoded, RepeatCount = 1 << EntryIndex;
                        if (RepeatCount > Count)
                        {
                            RepeatCount -= Count;
                            if (EntryIndex < EntriesCount)
                                for (int Iterator = CurrentCodeLength + 1; ++EntryIndex < EntriesCount; Iterator++)
                                {
                                    if ((RepeatCount <<= 1) <= LCountTable[Iterator])
                                        break;
                                    RepeatCount -= LCountTable[Iterator];
                                }
                        }
                        EntriesCount = 1 << EntryIndex;
                        if (TreesCount + EntriesCount > 1440)
                            throw new ValidatorException("Failed to decompress mod files");
                        TableStack[++TableLevel] = TableIndex = TreesCount;
                        TreesCount += EntriesCount;
                        if (TableLevel == 0)
                            TreeIndex = TableIndex;
                        else
                        {
                            BitOffsets[TableLevel] = CurrentCode;
                            TableEntry[0] = EntryIndex;
                            TableEntry[1] = BitsPerBranch;
                            int IndexOffset = CurrentCode >> (BitsDecoded - BitsPerBranch);
                            TableEntry[2] = TableIndex - TableStack[TableLevel - 1] - IndexOffset;
                            Copy(TableEntry, 0, Trees, (TableStack[TableLevel - 1] + IndexOffset) * 3, 3);
                        }
                    }
                    TableEntry[1] = CurrentCodeLength - BitsDecoded;
                    if (WorkspaceIndex >= TreeLength)
                        TableEntry[0] = 192;
                    else if (Workspace[WorkspaceIndex] < Offset)
                    {
                        TableEntry[0] = Workspace[WorkspaceIndex] < 256 ? 0 : 96;
                        TableEntry[2] = Workspace[WorkspaceIndex++];
                    }
                    else
                    {
                        TableEntry[0] = ExtraTable[Workspace[WorkspaceIndex] - Offset] + 80;
                        TableEntry[2] = Table[Workspace[WorkspaceIndex++] - Offset];
                    }
                    int Increment = 1 << (CurrentCodeLength - BitsDecoded);
                    for (int Iterator = CurrentCode >> BitsDecoded; Iterator < EntriesCount; Iterator += Increment)
                        Copy(TableEntry, 0, Trees, (TableIndex + Iterator) * 3, 3);
                    int XORMask = 1 << (CurrentCodeLength - 1);
                    for (; (CurrentCode & XORMask) != 0; XORMask >>= 1)
                        CurrentCode ^= XORMask;
                    CurrentCode ^= XORMask;
                    int Mask = (1 << BitsDecoded) - 1;
                    while ((CurrentCode & Mask) != BitOffsets[TableLevel])
                    {
                        TableLevel--;
                        Mask = (1 << (BitsDecoded -= BitsPerBranch)) - 1;
                    }
                }
            if (!(LastCountIncrease == 0 || MaxCodeLength == 1))
                throw new ValidatorException("Failed to decompress mod files");
        }
        internal void InflateBitTrees(int[] BitLengths, int[] Trees, ref int TreeIndex, out int BitsPerBranch)
        {
            BitsPerBranch = 7;
            TreesCount = 0;
            InitializeWorkspace(19);
            BuildTree(BitLengths, 0, 19, 19, null, null, ref TreeIndex, ref BitsPerBranch, Trees);
        }
        internal void InflateDynamicTrees(int DTreeLength, int LTreeLength, int[] BitLengths, int[] Trees, out int DBitsPerBranch, out int LBitsPerBranch, out int DTreeIndex, out int LTreeIndex)
        {
            DBitsPerBranch = 6;
            LBitsPerBranch = 9;
            TreesCount = LTreeIndex = DTreeIndex = 0;
            InitializeWorkspace(288);
            BuildTree(BitLengths, 0, LTreeLength, 257, CPLTable, CPLExtra, ref LTreeIndex, ref LBitsPerBranch, Trees);
            if (LBitsPerBranch == 0)
                throw new ValidatorException("Failed to decompress mod files");
            InitializeWorkspace(288);
            BuildTree(BitLengths, LTreeLength, DTreeLength, 0, CPDTable, CPDExtra, ref DTreeIndex, ref DBitsPerBranch, Trees);
            if (DBitsPerBranch == 0 && LTreeLength > 257)
                throw new ValidatorException("Failed to decompress mod files");
        }
    }
}