namespace TEKLauncher.Servers;

/// <summary>Server/cluster description container.</summary>
record Description
{
    /// <summary>Max level of wild creatures.</summary>
    public int? MaxDinoLvl { get; init; }
    /// <summary>Taming speed multiplier.</summary>
    public float? Taming { get; init; }
    /// <summary>Experience multiplier.</summary>
    public float? Experience { get; init; }
    /// <summary>Harvest amount multiplier.</summary>
    public float? Harvesting { get; init; }
    /// <summary>Egg hatch and/or baby mature speed multiplier.</summary>
    public float? Breeding { get; init; }
    /// <summary>The most common multiplier for item stack sizes.</summary>
    public float? Stacks { get; init; }
    /// <summary>Server owner-defined description lines.</summary>
    public string[]? Other { get; init; }
}