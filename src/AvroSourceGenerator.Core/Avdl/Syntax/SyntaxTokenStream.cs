using System.Collections.Immutable;

namespace AvroSourceGenerator.Avdl.Syntax;

internal sealed class SyntaxTokenStream(SourceText sourceText)
{
    // TODO:
    // We can probably make this lazy and only scan tokens as we need them instead of scanning the entire file upfront.
    // This would be more efficient if we want to fail fast on syntax errors and avoid scanning large files.
    private readonly ImmutableArray<SyntaxToken> _tokens = [.. new Scanner(sourceText).ScanAllTokens()];

    public SyntaxToken Current => Position < _tokens.Length ? _tokens[Position] : _tokens[^1];

    public int Position { get; private set; } = 0;

    public bool IsAtEnd => Current.SyntaxKind == SyntaxKind.EofToken;

    public SyntaxToken Match(SyntaxKind syntaxKind)
    {
        // Skip documentation trivia if we're not trying to match it. This allows us to ignore documentation comments in 
        // places where they are not expected without causing syntax errors, while still allowing us to capture them when we do want them.1
        while (syntaxKind != SyntaxKind.DocumentationTrivia && Current.SyntaxKind == SyntaxKind.DocumentationTrivia)
        {
            Next();
        }

        var syntaxToken = Current.SyntaxKind == syntaxKind
            ? Current
            : CreateInvalid();

        Position++;

        return syntaxToken;
    }

    private SyntaxToken CreateInvalid() => new(SyntaxKind.InvalidSyntax, new SourceSpan(sourceText, Position, 0));

    public SyntaxToken Peek(int offset = 0) => Position + offset < _tokens.Length ? _tokens[Position + offset] : _tokens[^1];

    public SyntaxToken Next() => Position < _tokens.Length ? _tokens[Position++] : _tokens[^1];

    public IEnumerable<SyntaxToken> GetTokens(int index = 0, int count = -1)
    {
        if (count == -1) count = _tokens.Length - index;
        for (var i = index; i < index + count && i < _tokens.Length; i++)
        {
            yield return _tokens[i];
        }
    }
}
