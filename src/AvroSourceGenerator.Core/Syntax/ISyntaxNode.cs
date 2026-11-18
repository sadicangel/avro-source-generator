namespace AvroSourceGenerator.Syntax;

internal interface ISyntaxNode
{
    SyntaxKind SyntaxKind { get; }
    IEnumerable<ISyntaxNode> Children();
}
