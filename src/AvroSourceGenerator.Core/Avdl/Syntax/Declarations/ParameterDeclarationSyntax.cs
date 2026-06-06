using AvroSourceGenerator.Avdl.Syntax.Types;

namespace AvroSourceGenerator.Avdl.Syntax.Declarations;

public sealed record class ParameterDeclarationSyntax(ITypeSyntax Type, SimpleNameSyntax Name, DefaultValueClauseSyntax? DefaultValueClause)
    : ISyntaxNode
{
    public SyntaxKind SyntaxKind => SyntaxKind.ParameterDeclaration;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return Type;
        yield return Name;
        if (DefaultValueClause is not null)
        {
            yield return DefaultValueClause;
        }
    }
}
