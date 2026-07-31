using AvroSourceGenerator.Avdl.Syntax.Annotations;
using AvroSourceGenerator.Avdl.Syntax.Types;

namespace AvroSourceGenerator.Avdl.Syntax.Declarations;

public sealed record class ParameterDeclarationSyntax(
    ITypeSyntax Type,
    SimpleNameSyntax Name,
    SyntaxList<DocumentationSyntax> Documentation,
    SyntaxList<IAnnotationSyntax> Annotations,
    DefaultValueClauseSyntax? DefaultValueClause)
    : IDeclarationSyntax
{
    public SyntaxKind SyntaxKind => SyntaxKind.ParameterDeclaration;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return Type;
        yield return Name;
        foreach (var documentation in Documentation)
            yield return documentation;
        foreach (var annotation in Annotations)
            yield return annotation;
        if (DefaultValueClause is not null)
        {
            yield return DefaultValueClause;
        }
    }
}
