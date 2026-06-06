using AvroSourceGenerator.Avdl.Syntax.Types;

namespace AvroSourceGenerator.Avdl.Syntax.Declarations;

public sealed record class ThrowsErrorClauseSyntax(SyntaxToken ThrowsKeyword, SeparatedSyntaxList<NamedTypeSyntax> Errors)
    : ISyntaxNode
{
    public SyntaxKind SyntaxKind => SyntaxKind.ThrowsErrorClause;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return ThrowsKeyword;
        foreach (var error in Errors)
        {
            yield return error;
        }
    }
}
