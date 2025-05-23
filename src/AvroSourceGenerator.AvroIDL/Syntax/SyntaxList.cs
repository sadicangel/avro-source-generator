using System.Collections;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace AvroSourceGenerator.AvroIDL.Syntax;

[CollectionBuilder(typeof(SyntaxListBuilder), nameof(SyntaxListBuilder.Create))]
public readonly record struct SyntaxList<T>(ImmutableArray<T> SyntaxNodes) : IReadOnlyList<T>
    where T : SyntaxNode
{
    public T this[int index] => SyntaxNodes[index];

    public int Count => SyntaxNodes.Length;

    public bool Equals(SyntaxList<T> other) => SyntaxNodes.SequenceEqual(other.SyntaxNodes);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var node in SyntaxNodes)
            hash.Add(node);
        return hash.ToHashCode();
    }

    public IEnumerator<T> GetEnumerator() => ((IReadOnlyList<T>)SyntaxNodes).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public static class SyntaxListBuilder
{
    public static SyntaxList<T> Create<T>(ReadOnlySpan<T> nodes) where T : SyntaxNode => new([.. nodes]);
}
