namespace TEKLauncher.Steam.Manifest;

/// <summary>Represents a Steam depot manifest file record.</summary>
readonly record struct FileEntry
{
    /// <summary>Gets relative path to the file.</summary>
    public string Name { get; init; }
    /// <summary>Gets total size of the file.</summary>
    public long Size { get; init; }
    /// <summary>Gets SHA-1 hash of file content.</summary>
    public Hash Hash { get; init; }
    /// <summary>Gets chunks that compose the file.</summary>
    public ChunkEntry[] Chunks { get; init; }
}