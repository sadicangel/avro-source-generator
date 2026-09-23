using AvroSourceGenerator.Avdl.Syntax;
using AvroSourceGenerator.Avdl.Syntax.Types;

namespace AvroSourceGenerator.Avdl.Declarations;

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
