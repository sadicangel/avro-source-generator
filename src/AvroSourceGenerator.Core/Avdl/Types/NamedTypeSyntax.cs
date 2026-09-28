namespace AvroSourceGenerator.Avdl.Types;

public sealed record class NamedTypeSyntax(INameSyntax Name) : ITypeSyntax
{
    public SyntaxKind SyntaxKind => SyntaxKind.NamedType;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return Name;
    }
}
