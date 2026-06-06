namespace AvroSourceGenerator.Avdl.Syntax.Types;

public interface ITypeSyntax : ISyntaxNode;

public sealed record class UnionTypeSyntax(
    SyntaxToken UnionKeyword,
    SyntaxToken BraceOpenToken,
    SeparatedSyntaxList<ITypeSyntax> Types,
    SyntaxToken BraceCloseToken) : ITypeSyntax
{
    public SyntaxKind SyntaxKind => SyntaxKind.UnionType;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return UnionKeyword;
        yield return BraceOpenToken;
        foreach (var type in Types.SyntaxNodes)
            yield return type;
        yield return BraceCloseToken;
    }
}
