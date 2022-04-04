namespace TEKLauncher.Utils;

/// <summary>CRC32 algorithm implementation.</summary>
static class CRC32
{
    /// <summary>Default CRC table.</summary>
    static readonly uint[] s_table = new uint[256];
    static CRC32()
    {
        //Initialize s_table
        for (uint i = 0; i < 256; i++)
        {
            uint entry = i;
            for (int j = 0; j < 8; j++)
            {
                bool bitSet = (entry & 1) != 0;
                entry >>= 1;
                if (bitSet)
                    entry ^= 0xEDB88320;
            }
            s_table[i] = entry;
        }
    }
    /// <summary>Computes CRC32 hash for specified stream.</summary>
    /// <param name="stream">Stream to compute the hash for; it will be read until end of stream is reached.</param>
    /// <returns>CRC32 hash value.</returns>
    public static uint ComputeHash(Stream stream)
    {
        uint hash = uint.MaxValue;
        Span<byte> buffer = stackalloc byte[81920];
        int bytesRead;
        do
        {
            bytesRead = stream.Read(buffer);
            for (int i = 0; i < bytesRead; i++)
                hash = unchecked((hash >> 8) ^ s_table[buffer[i] ^ hash & byte.MaxValue]);
        }
        while (bytesRead > 0);
        return ~hash;
    }
}