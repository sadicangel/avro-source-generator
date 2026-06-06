namespace AvroSourceGenerator.Avdl.Syntax;

public interface ISyntaxNode
{
    SyntaxKind SyntaxKind { get; }
    IEnumerable<ISyntaxNode> Children();
}
