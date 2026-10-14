namespace AvroSourceGenerator.Avdl;

internal interface ISyntaxNode
{
    SyntaxKind SyntaxKind { get; }
    IEnumerable<ISyntaxNode> Children();
}
