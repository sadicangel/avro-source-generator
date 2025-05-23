using System.Collections.Immutable;
using AvroSourceGenerator.AvroIDL.Syntax;

namespace AvroSourceGenerator.AvroIDL.Scanning;

partial class Scanner
{
    private static int ScanSyntaxTrivia(SyntaxTree syntaxTree, int position, bool leading, out SyntaxList<SyntaxTrivia> trivia)
    {
        var builder = ImmutableArray.CreateBuilder<SyntaxTrivia>();
        var totalScan = 0;
        while (true)
        {
            var item = default(SyntaxTrivia)!;
            var read = syntaxTree.SourceText.Text.AsSpan(position + totalScan) switch
            {
                ['/', '*', ..] => ScanMultiLineComment(syntaxTree, position, out item),
                ['/', '/', ..] => ScanSingleLineComment(syntaxTree, position, out item),
                ['\n' or '\r', ..] => ScanLineBreak(syntaxTree, position, out item),
                [' ' or '\t', ..] => ScanWhiteSpace(syntaxTree, position, out item),
                [var whitespace, ..] when char.IsWhiteSpace(whitespace) => ScanWhiteSpace(syntaxTree, position, out item),
                _ => 0
            };

            if (read == 0)
                break;

            totalScan += read;

            if (read > 0)
                builder.Add(item);

            if (item.SyntaxKind == SyntaxKind.LineBreakTrivia && !leading)
                break;
        }
        trivia = new SyntaxList<SyntaxTrivia>(builder.ToImmutable());

        return totalScan;
    }
}
