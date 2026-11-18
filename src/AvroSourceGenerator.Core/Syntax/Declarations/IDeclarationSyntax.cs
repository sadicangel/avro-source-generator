using AvroSourceGenerator.Syntax.Annotations;

namespace AvroSourceGenerator.Syntax.Declarations;

internal interface IDeclarationSyntax : ISyntaxNode
{
    SimpleNameSyntax Name { get; }
    SyntaxList<DocumentationSyntax> Documentation { get; }
    SyntaxList<IAnnotationSyntax> Annotations { get; }
}
