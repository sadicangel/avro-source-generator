using AvroSourceGenerator.Avdl.Annotations;
using AvroSourceGenerator.Avdl.Syntax;
using AvroSourceGenerator.Avdl.Syntax.Types;

namespace AvroSourceGenerator.Avdl.Types;

public sealed record class AnnotatedTypeSyntax(SyntaxList<IAnnotationSyntax> Annotations, ITypeSyntax Type) : ITypeSyntax
{
    public SyntaxKind SyntaxKind => SyntaxKind.AnnotatedType;

    public IEnumerable<ISyntaxNode> Children()
    {
        foreach (var annotation in Annotations)
            yield return annotation;
        yield return Type;
    }
}
