using System.Collections.Immutable;

namespace AvroSourceGenerator.Syntax;

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
}
