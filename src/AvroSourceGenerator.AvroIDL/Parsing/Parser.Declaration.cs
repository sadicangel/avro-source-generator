using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Syntax.Declarations;

namespace AvroSourceGenerator.AvroIDL.Parsing;

partial class Parser
{
    private static DeclarationSyntax ParseDeclaration(SyntaxTree syntaxTree, SyntaxIterator iterator) => iterator.Current.SyntaxKind switch
    {
        // Probably need to check for root Schema attribute.
        SyntaxKind.EnumKeyword => ParseEnumDeclaration(syntaxTree, iterator),
        SyntaxKind.FixedKeyword => ParseFixedDeclaration(syntaxTree, iterator),
        SyntaxKind.RecordKeyword => ParseRecordDeclaration(syntaxTree, iterator),
        SyntaxKind.ErrorKeyword => ParseErrorDeclaration(syntaxTree, iterator),
        _ => ParseProtocolDeclaration(syntaxTree, iterator),
    };
}
