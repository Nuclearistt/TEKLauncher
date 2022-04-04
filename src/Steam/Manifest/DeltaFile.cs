namespace TEKLauncher.Steam.Manifest;

/// <summary>Represents a Steam depot file changes record.</summary>
readonly record struct DeltaFile
{
    /// <summary>Gets relative path to the file.</summary>
    public string Name { get; init; }
    /// <summary>Gets a value that indicates whether the delta does not include all chunks assigned to the file entry in manifest.</summary>
    public bool Incomplete { get; init; }
    /// <summary>Gets total size of the file.</summary>
    public long FileSize { get; init; }
    /// <summary>Gets total uncompressed size of chunks in <see cref="Chunks"/>.</summary>
    public long ChunksSize { get; init; }
    /// <summary>Gets chunks that compose the delta.</summary>
    public ChunkEntry[] Chunks { get; init; }
}