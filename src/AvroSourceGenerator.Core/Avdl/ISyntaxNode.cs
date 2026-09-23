namespace AvroSourceGenerator.Avdl;

public interface ISyntaxNode
{
    SyntaxKind SyntaxKind { get; }
    IEnumerable<ISyntaxNode> Children();
}
