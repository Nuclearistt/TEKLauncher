namespace TEKLauncher.Utils.LZMA
{
    internal struct State
    {
        internal uint Index;
        internal bool IsCharState => Index < 7U;
        internal void UpdateChar()
        {
            if (Index < 4U)
                Index = 0U;
            else
                Index -= Index < 10U ? 3U : 6U;
        }
        internal void UpdateMatch() => Index = Index < 7U ? 7U : 10U;
        internal void UpdateRep() => Index = Index < 7U ? 8U : 11U;
        internal void UpdateShortRep() => Index = Index < 7U ? 9U : 11U;
    }
}