namespace AvroSourceGenerator.Avdl.Annotations;

public interface IAnnotationSyntax : ISyntaxNode
{
    public AnnotationNameSyntax AnnotationName { get; }

    public JsonValueSyntax JsonValue { get; }
}
