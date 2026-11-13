using System.Collections.Immutable;

namespace AvroSourceGenerator.Avdl;

internal sealed class SyntaxTokenStream(ImmutableArray<SyntaxToken> tokens, CancellationToken cancellationToken)
{
    public SyntaxToken Current => Position < tokens.Length ? tokens[Position] : tokens[^1];

    public int Position { get; private set; }

    public bool IsAtEnd => Current.SyntaxKind == SyntaxKind.EofToken;

    public SyntaxToken Peek(int offset = 0) => Position + offset < tokens.Length ? tokens[Position + offset] : tokens[^1];

    public SyntaxToken Next()
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Position < tokens.Length ? tokens[Position++] : tokens[^1];
    }

    public IEnumerable<SyntaxToken> GetTokens(int index = 0, int count = -1)
    {
        if (count == -1) count = tokens.Length - index;
        for (var i = index; i < index + count && i < tokens.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return tokens[i];
        }
    }
}
