using System.Collections;
using System.Collections.Immutable;

namespace AvroSourceGenerator.Avdl;

public readonly record struct SeparatedSyntaxList<T>(ImmutableArray<ISyntaxNode> SyntaxNodes)
    : IReadOnlyList<T> where T : ISyntaxNode
{
    public int Count { get => (SyntaxNodes.Length + 1) / 2; }

    public T this[int index] => (T)SyntaxNodes[index * 2];

    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < Count; ++i)
            yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Equals(SeparatedSyntaxList<T> other) => SyntaxNodes.SequenceEqual(other.SyntaxNodes);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var node in SyntaxNodes)
            hash.Add(node);
        return hash.ToHashCode();
    }
}
