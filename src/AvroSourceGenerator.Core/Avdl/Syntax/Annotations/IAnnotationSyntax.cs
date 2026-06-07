namespace AvroSourceGenerator.Avdl.Syntax.Annotations;

public interface IAnnotationSyntax : ISyntaxNode
{
    public JsonValueSyntax JsonValue { get; }
}
