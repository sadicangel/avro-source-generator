using AvroSourceGenerator.Syntax.Annotations;

namespace AvroSourceGenerator.Syntax.Types;

internal sealed record class AnnotatedTypeSyntax(SyntaxList<IAnnotationSyntax> Annotations, ITypeSyntax Type) : ITypeSyntax
{
    public SyntaxKind SyntaxKind => SyntaxKind.AnnotatedType;

    public IEnumerable<ISyntaxNode> Children()
    {
        foreach (var annotation in Annotations)
            yield return annotation;
        yield return Type;
    }
}
