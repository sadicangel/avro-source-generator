using AvroSourceGenerator.AvroIDL.Syntax;

namespace AvroSourceGenerator.AvroIDL.Parsing;
partial class Parser
{
    private static SyntaxNode ParseNode(SyntaxTree syntaxTree, SyntaxIterator iterator) => iterator.Current.SyntaxKind switch
    {
        SyntaxKind.AtSignToken => ParseAnnotation(syntaxTree, iterator) ?? throw new InvalidOperationException("Unreachable"),
        SyntaxKind.ImportKeyword => ParseImport(syntaxTree, iterator),
        _ => ParseDeclaration(syntaxTree, iterator),
    };
}
