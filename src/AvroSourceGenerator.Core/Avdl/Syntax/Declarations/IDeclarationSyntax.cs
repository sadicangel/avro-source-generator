using AvroSourceGenerator.Avdl.Syntax.Annotations;

namespace AvroSourceGenerator.Avdl.Syntax.Declarations;

public interface IDeclarationSyntax : ISyntaxNode
{
    SimpleNameSyntax Name { get; }
    SyntaxList<DocumentationSyntax> Documentation { get; }
    SyntaxList<IAnnotationSyntax> Annotations { get; }
}
