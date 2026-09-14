using System.Collections.Immutable;

namespace AvroSourceGenerator.Avdl.Syntax;

public readonly record struct SeparatedSyntaxList<T>
    : IEquatable<SeparatedSyntaxList<T>>, IReadOnlyList<T> where T : ISyntaxNode
{
    private readonly ImmutableArray<ISyntaxNode> _syntaxNodes;

    public SeparatedSyntaxList(ImmutableArray<ISyntaxNode> syntaxNodes) => _syntaxNodes = syntaxNodes;

    public ImmutableArray<ISyntaxNode> SyntaxNodes => _syntaxNodes.IsDefault ? [] : _syntaxNodes;

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

    public void Deconstruct(out ImmutableArray<ISyntaxNode> syntaxNodes) => syntaxNodes = SyntaxNodes;
}
