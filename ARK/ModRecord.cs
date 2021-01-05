using static TEKLauncher.Data.LocalizationManager;

namespace TEKLauncher.ARK
{
    internal struct ModRecord
    {
        internal ModRecord(ulong ID, string Name, string Size)
        {
            this.ID = ID;
            this.Name = Name;
            string Unit = null;
            switch (Size.Substring(Size.Length - 2))
            {
                case "KB": Unit = LocString(LocCode.KB); break;
                case "MB": Unit = LocString(LocCode.MB); break;
                case "GB": Unit = LocString(LocCode.GB); break;
            }
            this.Size = $"{Size.Substring(0, Size.Length - 2)} {Unit}";
        }
        internal ulong ID;
        internal string Name, Size;
    }
}