using AvroSourceGenerator.AvroIDL.Syntax;

namespace AvroSourceGenerator.AvroIDL.Parsing;
partial class Parser
{
    private static ImportSyntax ParseImport(SyntaxTree syntaxTree, SyntaxIterator iterator)
    {
        var importKeyword = iterator.Match(SyntaxKind.ImportKeyword);
        var importTypeKeyword = iterator.Match(SyntaxKind.IdlKeyword, SyntaxKind.SchemaKeyword, SyntaxKind.ProtocolKeyword);
        var fileNameLiteralToken = iterator.Match(SyntaxKind.StringLiteralToken);
        var semicolonToken = iterator.Match(SyntaxKind.SemicolonToken);

        return new ImportSyntax(
            syntaxTree,
            importKeyword,
            importTypeKeyword,
            fileNameLiteralToken,
            semicolonToken);
    }
}
