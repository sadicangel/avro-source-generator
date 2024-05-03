using System.Diagnostics.CodeAnalysis;

namespace AvroNet.Schemas;

internal sealed class ReadOnlyMemoryComparer : IEqualityComparer<ReadOnlyMemory<byte>>
{
    public bool Equals(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y) => x.Span.SequenceEqual(y.Span);
    public int GetHashCode([DisallowNull] ReadOnlyMemory<byte> obj)
    {
        var hash = new HashCode();
#if NET5_0_OR_GREATER
        hash.AddBytes(obj.Span);
#else
        foreach (var b in obj.Span)
            hash.Add(b);
#endif
        return hash.ToHashCode();
    }
}