using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Output;

internal sealed record class SourceTextWriterContext(
    string Name,
    string Namespace,
    string SchemaJson,
    string AccessModifier,
    string DeclarationType,
    AvroModelFeatures Features)
{
    public SchemaRegistry Schemas { get; } = new();

    public bool UseNullableReferenceTypes { get => (Features & AvroModelFeatures.NullableReferenceTypes) != 0; }
    public bool UseFileScopedNamespaces { get => (Features & AvroModelFeatures.FileScopedNamespaces) != 0; }
    public bool UseRequiredProperties { get => (Features & AvroModelFeatures.RequiredProperties) != 0; }
    public bool UseInitOnlyProperties { get => (Features & AvroModelFeatures.InitOnlyProperties) != 0; }
    public bool UseUnsafeAccessors { get => (Features & AvroModelFeatures.UnsafeAccessors) != 0; }
}
