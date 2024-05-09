namespace AvroNet;

internal sealed record class AvroModelOptions(
    string Name,
    string Schema,
    string Namespace,
    string AccessModifier,
    string DeclarationType,
    AvroModelFeatures Features)
{
    public bool UseNullableReferenceTypes { get => (Features & AvroModelFeatures.NullableReferenceTypes) != 0; }
    public bool UseFileScopedNamespaces { get => (Features & AvroModelFeatures.FileScopedNamespaces) != 0; }
    public bool UseRequiredProperties { get => (Features & AvroModelFeatures.RequiredProperties) != 0; }
    public bool UseInitOnlyProperties { get => (Features & AvroModelFeatures.InitOnlyProperties) != 0; }
    public bool UseUnsafeAccessors { get => (Features & AvroModelFeatures.UnsafeAccessors) != 0; }
}
