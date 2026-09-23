namespace AvroSourceGenerator.Avdl.Syntax.Types;

public sealed record class OptionalTypeSyntax(ITypeSyntax Type, SyntaxToken QuestionMarkToken) : ITypeSyntax
{
    public SyntaxKind SyntaxKind => SyntaxKind.OptionalType;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return Type;
        yield return QuestionMarkToken;
    }
}
