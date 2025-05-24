using System.Collections.Immutable;
using AvroSourceGenerator.AvroIDL.Syntax;

namespace AvroSourceGenerator.AvroIDL.Scanning;

partial class Scanner
{
    private static int ScanSyntaxTrivia(SyntaxTree syntaxTree, int offset, bool leading, out SyntaxList<SyntaxTrivia> trivia)
    {
        var builder = ImmutableArray.CreateBuilder<SyntaxTrivia>();
        var length = 0;
        while (true)
        {
            var item = default(SyntaxTrivia)!;
            var read = syntaxTree.SourceText.Text.AsSpan(offset + length) switch
            {
                ['/', '*', ..] => ScanMultiLineComment(syntaxTree, offset + length, out item),
                ['/', '/', ..] => ScanSingleLineComment(syntaxTree, offset + length, out item),
                ['\n' or '\r', ..] => ScanLineBreak(syntaxTree, offset + length, out item),
                [' ' or '\t', ..] => ScanWhiteSpace(syntaxTree, offset + length, out item),
                [var whitespace, ..] when char.IsWhiteSpace(whitespace) => ScanWhiteSpace(syntaxTree, offset + length, out item),
                _ => 0
            };

            if (read == 0)
                break;

            length += read;

            builder.Add(item);

            if (item.SyntaxKind == SyntaxKind.LineBreakTrivia && !leading)
                break;
        }
        trivia = new SyntaxList<SyntaxTrivia>(builder.ToImmutable());

        return length;
    }
}
