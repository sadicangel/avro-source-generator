using System.Collections.Immutable;
using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Syntax.Declarations;

namespace AvroSourceGenerator.AvroIDL.Parsing;

partial class Parser
{
    private static ProtocolDeclarationSyntax ParseProtocolDeclaration(SyntaxTree syntaxTree, SyntaxIterator iterator)
    {
        var annotations = ParseSyntaxList(syntaxTree, iterator, ParseAnnotation);
        var protocolKeyword = iterator.Match(SyntaxKind.ProtocolKeyword);
        var name = ParseSimpleName(syntaxTree, iterator);
        var braceOpenToken = iterator.Match(SyntaxKind.BraceOpenToken);
        var types = ImmutableArray.CreateBuilder<SchemaDeclarationSyntax>();
        var messages = ImmutableArray.CreateBuilder<MessageDeclarationSyntax>();
        ParseSyntaxList(syntaxTree, iterator, [SyntaxKind.BraceCloseToken], ParseSchemaOrMessageDeclaration, node =>
        {
            switch (node)
            {
                case SchemaDeclarationSyntax schema: types.Add(schema); break;
                case MessageDeclarationSyntax message: messages.Add(message); break;
                default: throw new InvalidOperationException($"Unexpected node type: {node.SyntaxKind}");
            }
        });
        var braceCloseToken = iterator.Match(SyntaxKind.BraceCloseToken);

        return new ProtocolDeclarationSyntax(
            syntaxTree,
            protocolKeyword,
            name,
            braceOpenToken,
            new SyntaxList<SchemaDeclarationSyntax>(types.ToImmutable()),
            new SyntaxList<MessageDeclarationSyntax>(messages.ToImmutable()),
            braceCloseToken);

        static SyntaxNode ParseSchemaOrMessageDeclaration(SyntaxTree syntaxTree, SyntaxIterator iterator) => iterator.Current.SyntaxKind switch
        {
            SyntaxKind.EnumKeyword => ParseEnumDeclaration(syntaxTree, iterator),
            SyntaxKind.FixedKeyword => ParseFixedDeclaration(syntaxTree, iterator),
            SyntaxKind.RecordKeyword => ParseRecordDeclaration(syntaxTree, iterator),
            SyntaxKind.ErrorKeyword => ParseErrorDeclaration(syntaxTree, iterator),
            _ => ParseMessageDeclaration(syntaxTree, iterator),
        };
    }
}
