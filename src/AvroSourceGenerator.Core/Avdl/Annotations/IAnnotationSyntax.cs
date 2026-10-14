namespace AvroSourceGenerator.Avdl.Annotations;

internal interface IAnnotationSyntax : ISyntaxNode
{
    public AnnotationNameSyntax AnnotationName { get; }

    // Retain the common value contract for consumers of annotation syntax.
    // ReSharper disable once UnusedMemberInSuper.Global
    public JsonValueSyntax JsonValue { get; }
}
