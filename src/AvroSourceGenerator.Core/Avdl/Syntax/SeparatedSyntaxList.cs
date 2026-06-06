using System.Collections.Immutable;

namespace AvroSourceGenerator.Avdl.Syntax;

public readonly record struct SeparatedSyntaxList<T>(ImmutableArray<ISyntaxNode> SyntaxNodes)
    : IEquatable<SeparatedSyntaxList<T>>, IReadOnlyList<T> where T : ISyntaxNode
{
    public int Count { get => (SyntaxNodes.Length + 1) / 2; }

    public T this[int index] => (T)SyntaxNodes[index * 2];

    internal SyntaxToken GetSeparator(Index index) => (SyntaxToken)SyntaxNodes[index.GetOffset(SyntaxNodes.Length) * 2 + 1];

    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < Count; ++i)
            yield return this[i];
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Equals(SeparatedSyntaxList<T> other) => SyntaxNodes.SequenceEqual(other.SyntaxNodes);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var node in SyntaxNodes)
            hash.Add(node);
        return hash.ToHashCode();
    }
}
