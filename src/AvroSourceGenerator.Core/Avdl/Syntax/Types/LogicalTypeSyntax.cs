namespace AvroSourceGenerator.Avdl.Syntax.Types;

public sealed record class LogicalTypeSyntax(SyntaxToken LogicalTypeNameKeyword) : ILogicalTypeSyntax
{
    public SyntaxKind SyntaxKind => SyntaxKind.LogicalType;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return LogicalTypeNameKeyword;
    }
}
