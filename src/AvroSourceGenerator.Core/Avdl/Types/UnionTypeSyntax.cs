using AvroSourceGenerator.Avdl.Syntax;
using AvroSourceGenerator.Avdl.Syntax.Types;

namespace AvroSourceGenerator.Avdl.Types;

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
