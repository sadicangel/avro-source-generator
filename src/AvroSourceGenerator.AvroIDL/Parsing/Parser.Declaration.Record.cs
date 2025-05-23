using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Syntax.Declarations;

namespace AvroSourceGenerator.AvroIDL.Parsing;

partial class Parser
{
    private static RecordDeclarationSyntax ParseRecordDeclaration(SyntaxTree syntaxTree, SyntaxIterator iterator)
    {
        var recordKeyword = iterator.Match(SyntaxKind.RecordKeyword);
        var name = ParseSimpleName(syntaxTree, iterator);
        var braceOpenToken = iterator.Match(SyntaxKind.BraceOpenToken);
        var fields = ParseSyntaxList(syntaxTree, iterator, [SyntaxKind.BraceCloseToken], ParseFieldDeclaration);
        var braceCloseToken = iterator.Match(SyntaxKind.BraceCloseToken);

        return new RecordDeclarationSyntax(
            syntaxTree,
            recordKeyword,
            name,
            braceOpenToken,
            fields,
            braceCloseToken);
    }
}
