namespace TEKLauncher.Steam.Manifest;

/// <summary>Represents Steam depot patch chunk record.</summary>
readonly record struct PatchChunkEntry
{
    /// <summary>Gets GID of the source chunk.</summary>
    public Hash SourceGid { get; init; }
    /// <summary>Gets GID of the target chunk.</summary>
    public Hash TargetGid { get; init; }
    /// <summary>Gets patching data.</summary>
    public ReadOnlyMemory<byte> Data { get; init; }
}