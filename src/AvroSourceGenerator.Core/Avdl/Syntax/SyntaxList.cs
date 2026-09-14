using System.Collections;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace AvroSourceGenerator.Avdl.Syntax;

[CollectionBuilder(typeof(SyntaxListBuilder), nameof(SyntaxListBuilder.Create))]
public readonly record struct SyntaxList<T> : IReadOnlyList<T> where T : ISyntaxNode
{
    private readonly ImmutableArray<T> _syntaxNodes;

    public SyntaxList(ImmutableArray<T> syntaxNodes) => _syntaxNodes = syntaxNodes;

    public ImmutableArray<T> SyntaxNodes => _syntaxNodes.IsDefault ? [] : _syntaxNodes;

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

    public void Deconstruct(out ImmutableArray<T> syntaxNodes) => syntaxNodes = SyntaxNodes;
}

public static class SyntaxListBuilder
{
    public static SyntaxList<T> Create<T>(ReadOnlySpan<T> nodes) where T : ISyntaxNode => new([.. nodes]);
}
