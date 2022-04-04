namespace TEKLauncher.Steam.Manifest;

/// <summary>Represents a Steam depot manifest chunk record.</summary>
readonly record struct ChunkEntry
{
    /// <summary>Gets GID of the chunk.</summary>
    public Hash Gid { get; init; }
    /// <summary>Gets Adler hash of chunk data.</summary>
    public uint Hash { get; init; }
    /// <summary>Gets offset of chunk data from the beginning of containing file.</summary>
    public long Offset { get; init; }
    /// <summary>Gets size of LZMA-compressed chunk data.</summary>
    public int CompressedSize { get; init; }
    /// <summary>Gets size of uncompressed chunk data.</summary>
    public int UncompressedSize { get; init; }
}