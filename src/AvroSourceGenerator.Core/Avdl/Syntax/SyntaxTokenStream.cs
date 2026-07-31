using System.Collections.Immutable;
using AvroSourceGenerator.Avdl.Diagnostics;
using AvroSourceGenerator.Avdl.Text;

namespace AvroSourceGenerator.Avdl.Syntax;

internal sealed class SyntaxTokenStream
{
    // TODO:
    // We can probably make this lazy and only scan tokens as we need them instead of scanning the entire file upfront.
    // This would be more efficient if we want to fail fast on syntax errors and avoid scanning large files.
    private readonly SourceText _sourceText;
    private readonly ImmutableArray<SyntaxToken> _tokens;
    private readonly List<SyntaxDiagnostic> _diagnostics = [];
    private bool _lastTokenWasSynthetic = false;

    public SyntaxTokenStream(SourceText sourceText)
    {
        _sourceText = sourceText;
        var scanner = new Scanner(sourceText);
        _tokens = [.. scanner.ScanAllTokens()];
        _diagnostics.AddRange(scanner.Diagnostics);
    }

    public SyntaxToken Current => Position < _tokens.Length ? _tokens[Position] : _tokens[^1];

    public int Position { get; private set; } = 0;

    public bool IsAtEnd => Current.SyntaxKind == SyntaxKind.EofToken;

    public IReadOnlyList<SyntaxDiagnostic> Diagnostics => _diagnostics;

    public void Report(SyntaxDiagnostic diagnostic) => _diagnostics.Add(diagnostic);

    public SyntaxToken Match(SyntaxKind syntaxKind)
    {
        // Skip documentation trivia if we're not trying to match it. This allows us to ignore documentation comments in 
        // places where they are not expected without causing syntax errors, while still allowing us to capture them when we do want them.1
        while (syntaxKind != SyntaxKind.DocumentationTrivia && Current.SyntaxKind == SyntaxKind.DocumentationTrivia)
        {
            Next();
        }

        if (Current.SyntaxKind == syntaxKind)
        {
            return Next();
        }

        if (CanSkipCurrentToMatch(syntaxKind))
        {
            ReportUnexpectedToken(syntaxKind, Current);
            _ = Next();
            return Next();
        }

        return CreateSynthetic(syntaxKind);
    }

    private SyntaxToken CreateSynthetic(SyntaxKind expectedSyntaxKind)
    {
        ReportUnexpectedToken(expectedSyntaxKind, Current);
        _lastTokenWasSynthetic = true;
        return new SyntaxToken(expectedSyntaxKind, new SourceSpan(_sourceText, Current.SourceSpan.Offset, 0));
    }

    private void ReportUnexpectedToken(SyntaxKind expectedSyntaxKind, SyntaxToken actual)
    {
        if (!_lastTokenWasSynthetic)
            _diagnostics.Add(SyntaxDiagnostic.UnexpectedToken(expectedSyntaxKind, actual));
    }

    private bool CanSkipCurrentToMatch(SyntaxKind syntaxKind) =>
        !IsAtEnd
        && Peek(1).SyntaxKind == syntaxKind
        && !IsStructuralRecoveryAnchor(Current.SyntaxKind);

    private static bool IsStructuralRecoveryAnchor(SyntaxKind syntaxKind) =>
        syntaxKind is
            SyntaxKind.EofToken
            or SyntaxKind.BraceOpenToken
            or SyntaxKind.BraceCloseToken
            or SyntaxKind.ParenthesisOpenToken
            or SyntaxKind.ParenthesisCloseToken
            or SyntaxKind.BracketOpenToken
            or SyntaxKind.BracketCloseToken
            or SyntaxKind.LessThanToken
            or SyntaxKind.GreaterThanToken
            or SyntaxKind.AtSignToken
            or SyntaxKind.CommaToken
            or SyntaxKind.DotToken
            or SyntaxKind.ColonToken
            or SyntaxKind.SemicolonToken
            or SyntaxKind.EqualsToken
            or SyntaxKind.QuestionMarkToken;

    public SyntaxToken Peek(int offset = 0) => Position + offset < _tokens.Length ? _tokens[Position + offset] : _tokens[^1];

    public SyntaxToken Next()
    {
        var token = Position < _tokens.Length ? _tokens[Position++] : _tokens[^1];
        _lastTokenWasSynthetic = false;
        return token;
    }

    public IEnumerable<SyntaxToken> GetTokens(int index = 0, int count = -1)
    {
        if (count == -1) count = _tokens.Length - index;
        for (var i = index; i < index + count && i < _tokens.Length; i++)
        {
            yield return _tokens[i];
        }
    }
}
