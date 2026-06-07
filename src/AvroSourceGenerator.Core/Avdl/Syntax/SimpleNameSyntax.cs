namespace AvroSourceGenerator.Avdl.Syntax;

public sealed record class SimpleNameSyntax(SyntaxToken Identifier) : INameSyntax
{
    public SyntaxKind SyntaxKind => SyntaxKind.SimpleName;

    public string FullName => field ??= Identifier.ValueText;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return Identifier;
    }
}
