using System.Runtime.InteropServices;

namespace TEKLauncher.Utils;

/// <summary>Envelopes a SHA-1 hash and provides significantly faster comparisons.</summary>
[StructLayout(LayoutKind.Explicit)]
readonly record struct Hash
{
    /// <summary>Pointer to an <see cref="Inner"/> struct on the same memory location as <see cref="Data"/>.</summary>
    [FieldOffset(0)]
    readonly ReadOnlyMemory<Inner> _inner = default;
    /// <summary>Pointer to the binary hash data.</summary>
    [FieldOffset(0)]
    public readonly ReadOnlyMemory<byte> Data;
    /// <summary>Initializes a new <see cref="Hash"/> structure enveloping <paramref name="data"/>.</summary>
    /// <param name="data">Hash data to envelope.</param>
    public Hash(ReadOnlyMemory<byte> data) => Data = data;
    /// <summary>Writes string representation of the hash to a character buffer.</summary>
    /// <param name="buffer">Buffer that string representation of the hash will be written to.</param>
    public void WriteTo(Span<char> buffer)
    {
        var span = Data.Span;
        for (int i = 0; i < 20; i++)
            span[i].TryFormat(buffer[(i * 2)..], out _, "X2");
    }
    //1 dword + 2 qword cmps instead of 20 byte cmps.</summary>
    public bool Equals(Hash other)
    {
        var thisInner = _inner.Span[0];
        var otherInner = other._inner.Span[0];
        return thisInner.F3 == otherInner.F3 && thisInner.F2 == otherInner.F2 && thisInner.F1 == otherInner.F1;
    }
    /// <summary>Compares this instance to a specified hash and returns an indication of their relative values.</summary>
    /// <param name="other">A hash to compare.</param>
    /// <returns>
    /// A negative value if <see langword="this"/> is less than <paramref name="other"/>.<br/>
    /// Zero if <see langword="this"/> is equal to <paramref name="other"/>.<br/>
    /// A positive value if <see langword="this"/> is greater than <paramref name="other"/>.
    /// </returns>
    public int CompareTo(Hash other)
    {
        var thisInner = _inner.Span[0];
        var otherInner = other._inner.Span[0];
        int difference = thisInner.F3.CompareTo(otherInner.F3);
        if (difference == 0)
        {
            difference = thisInner.F2.CompareTo(otherInner.F2);
            if (difference == 0)
                difference = thisInner.F1.CompareTo(otherInner.F1);
        }
        return difference;
    }
    public override int GetHashCode() => Data.GetHashCode();
    public override string ToString()
    {
        Span<char> chars = stackalloc char[40];
        WriteTo(chars);
        return new(chars);
    }
#pragma warning disable CS0649
    /// <summary>Alternative representation of a SHA-1 hash, 2 qwords + dword instead of 20 bytes.</summary>
    readonly record struct Inner
    {
        public readonly ulong F1, F2;
        public readonly uint F3;
    }
#pragma warning restore CS0649
    /// <summary>Stack variant of the <see cref="Hash"/> structure.</summary>
    [StructLayout(LayoutKind.Explicit)]
    public readonly ref struct StackHash
    {
        /// <summary>Pointer to the binary hash data.</summary>
        [FieldOffset(0)]
        readonly ReadOnlySpan<byte> _data;
        /// <summary>Pointer to an <see cref="Inner"/> struct on the same memory location as <see cref="_data"/>.</summary>
        [FieldOffset(0)]
        readonly ReadOnlySpan<Inner> _inner = default;
        /// <summary>Initializes a new <see cref="StackHash"/> structure enveloping <paramref name="data"/>.</summary>
        /// <param name="data">Hash data to envelope.</param>
        public StackHash(ReadOnlySpan<byte> data) => _data = data;
        public override bool Equals(object? obj) => obj is Hash hash && this == hash;
        public override int GetHashCode() => _data.GetHashCode();
        public static bool operator ==(StackHash stackHash, Hash hash)
        {
            var stackHashInner = stackHash._inner[0];
            var hashInner = hash._inner.Span[0];
            return stackHashInner.F3 == hashInner.F3 && stackHashInner.F2 == hashInner.F2 && stackHashInner.F1 == hashInner.F1;
        }
        public static bool operator !=(StackHash stackHash, Hash hash)
        {
            var stackHashInner = stackHash._inner[0];
            var hashInner = hash._inner.Span[0];
            return stackHashInner.F3 != hashInner.F3 || stackHashInner.F2 != hashInner.F2 || stackHashInner.F1 != hashInner.F1;
        }
    }
}