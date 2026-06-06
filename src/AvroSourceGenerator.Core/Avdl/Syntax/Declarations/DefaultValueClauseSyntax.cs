namespace AvroSourceGenerator.Avdl.Syntax.Declarations;

public sealed record class DefaultValueClauseSyntax(SyntaxToken EqualsToken, JsonValueSyntax JsonValue) : ISyntaxNode
{
    public SyntaxKind SyntaxKind => SyntaxKind.DefaultValueClause;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return EqualsToken;
        yield return JsonValue;
    }
}
