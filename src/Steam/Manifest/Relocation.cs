namespace TEKLauncher.Steam.Manifest;

/// <summary>Represents a record describing relocation of chunks within a file.</summary>
readonly record struct Relocation
{
    /// <summary>Gets relative path to the file.</summary>
    public string FileName { get; init; }
    /// <summary>Gets relocation entries for the file.</summary>
    public Entry[] Entries { get; init; }
    public readonly record struct Entry
    {
        /// <summary>Gets offset of the chunk from the beginning of the file before relocation.</summary>
        public long OldOffset { get; init; }
        /// <summary>Gets offset of the chunk from the beginning of the file after relocation.</summary>
        public long NewOffset { get; init; }
        /// <summary>Gets size of the chunk in bytes.</summary>
        public int Size { get; init; }
    }
}