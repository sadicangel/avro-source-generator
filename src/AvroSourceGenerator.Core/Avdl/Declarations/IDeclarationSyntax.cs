using AvroSourceGenerator.Avdl.Annotations;

namespace AvroSourceGenerator.Avdl.Declarations;

internal interface IDeclarationSyntax : ISyntaxNode
{
    SimpleNameSyntax Name { get; }
    SyntaxList<DocumentationSyntax> Documentation { get; }
    SyntaxList<IAnnotationSyntax> Annotations { get; }
}
