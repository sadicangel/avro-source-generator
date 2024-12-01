using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Output;

internal sealed record class SourceTextWriterOptions(
    string AvroSchema,
    string Name,
    string Namespace,
    string Declaration,
    LanguageFeatures LanguageFeatures)
{
    public SchemaRegistry Schemas { get; } = new();

    public bool UseNullableReferenceTypes { get => (LanguageFeatures & LanguageFeatures.NullableReferenceTypes) != 0; }
    public bool UseFileScopedNamespaces { get => (LanguageFeatures & LanguageFeatures.FileScopedNamespaces) != 0; }
    public bool UseRequiredProperties { get => (LanguageFeatures & LanguageFeatures.RequiredProperties) != 0; }
    public bool UseInitOnlyProperties { get => (LanguageFeatures & LanguageFeatures.InitOnlyProperties) != 0; }
    public bool UseUnsafeAccessors { get => (LanguageFeatures & LanguageFeatures.UnsafeAccessors) != 0; }
}
