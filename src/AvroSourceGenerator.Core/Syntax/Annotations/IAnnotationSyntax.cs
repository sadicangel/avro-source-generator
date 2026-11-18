using AvroSourceGenerator.Syntax.Json;

namespace AvroSourceGenerator.Syntax.Annotations;

internal interface IAnnotationSyntax : ISyntaxNode
{
    public AnnotationNameSyntax AnnotationName { get; }

    public JsonValueSyntax JsonValue { get; }
}
