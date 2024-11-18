using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Output;

internal sealed record class SourceTextWriterOptions(
    string Name,
    string Namespace,
    string AvroSchema,
    string AccessModifier,
    string DeclarationType,
    LanguageFeatures Features)
{
    public SchemaRegistry Schemas { get; } = new();

    public bool UseNullableReferenceTypes { get => (Features & LanguageFeatures.NullableReferenceTypes) != 0; }
    public bool UseFileScopedNamespaces { get => (Features & LanguageFeatures.FileScopedNamespaces) != 0; }
    public bool UseRequiredProperties { get => (Features & LanguageFeatures.RequiredProperties) != 0; }
    public bool UseInitOnlyProperties { get => (Features & LanguageFeatures.InitOnlyProperties) != 0; }
    public bool UseUnsafeAccessors { get => (Features & LanguageFeatures.UnsafeAccessors) != 0; }
}
