namespace TEKLauncher.Steam;

/// <summary>Represents an identifier for Steam item (either depot or depot-mod pair)</summary>
record ItemIdentifier
{
    /// <summary>Initializes a new item identifier with specified depot ID.</summary>
    /// <param name="depotId">ID of the depot.</param>
    public ItemIdentifier(uint depotId)
    {
        DepotId = depotId;
        ModId = 0;
    }
    /// <summary>Initializes a new item identifier with workshop depot ID (346110) and specified mod ID.</summary>
    /// <param name="modId">ID of the mod.</param>
    public ItemIdentifier(ulong modId)
    {
        DepotId = 346110;
        ModId = modId;
    }
    /// <summary>Initializes a new item identifier by parsing its string representation.</summary>
    /// <param name="str">String representation of item identifier.</param>
    public ItemIdentifier(string str)
    {
        if (str.Contains('.'))
        {
            string[] substrings = str.Split('.');
            DepotId = uint.Parse(substrings[0]);
            ModId = ulong.Parse(substrings[1]);

        }
        else
        {
            DepotId = uint.Parse(str);
            ModId = 0;
        }
    }
    /// <summary>Gets depot ID of the identifier.</summary>
    public uint DepotId { get; init; }
    /// <summary>Gets mod ID of the identifier.</summary>
    public ulong ModId { get; init; }
    public override string ToString() => DepotId == 346110 ? string.Concat("346110.", ModId.ToString()) : DepotId.ToString();
}