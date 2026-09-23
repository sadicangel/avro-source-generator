using AvroSourceGenerator.Avdl.Annotations;
using AvroSourceGenerator.Avdl.Syntax;

namespace AvroSourceGenerator.Avdl.Declarations;

public interface IDeclarationSyntax : ISyntaxNode
{
    SimpleNameSyntax Name { get; }
    SyntaxList<DocumentationSyntax> Documentation { get; }
    SyntaxList<IAnnotationSyntax> Annotations { get; }
}
