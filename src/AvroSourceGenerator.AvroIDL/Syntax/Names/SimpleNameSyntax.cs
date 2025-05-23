namespace AvroSourceGenerator.AvroIDL.Syntax.Names;
public record class SimpleNameSyntax(
    SyntaxTree SyntaxTree,
    SyntaxToken IdentifierToken)
    : NameSyntax(SyntaxKind.SimpleName, SyntaxTree)
{
    public override string FullName => field ??= IdentifierToken.SourceSpan.ToString();

    public override IEnumerable<SyntaxNode> Children()
    {
        yield return IdentifierToken;
    }
}
