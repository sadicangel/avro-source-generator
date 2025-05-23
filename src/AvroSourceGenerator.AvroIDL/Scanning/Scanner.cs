using AvroSourceGenerator.AvroIDL.Syntax;

namespace AvroSourceGenerator.AvroIDL.Scanning;

internal static partial class Scanner
{
    internal static IEnumerable<SyntaxToken> Scan(SyntaxTree syntaxTree)
    {
        var badTokens = new List<SyntaxToken>();
        var position = 0;
        SyntaxToken token;
        do
        {
            position += ScanSyntaxToken(syntaxTree, position, out token);
            if (token.SyntaxKind is SyntaxKind.InvalidSyntax)
            {
                badTokens.Add(token);
                continue;
            }

            if (badTokens.Count > 0)
            {
                token = token with
                {
                    LeadingTrivia = new([
                        ..badTokens.SelectMany(ToInvalidTextTrivia),
                        ..token.LeadingTrivia
                    ])
                };
                badTokens.Clear();

                static IEnumerable<SyntaxTrivia> ToInvalidTextTrivia(SyntaxToken token)
                {
                    foreach (var trivia in token.LeadingTrivia)
                        yield return trivia;
                    yield return new SyntaxTrivia(SyntaxKind.InvalidTextTrivia, token.SyntaxTree, token.SourceSpan);
                    foreach (var trivia in token.TrailingTrivia)
                        yield return trivia;
                }
            }
            yield return token;
        }
        while (token.SyntaxKind is not SyntaxKind.EofToken);
    }
}
