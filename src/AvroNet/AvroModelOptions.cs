
using Microsoft.CodeAnalysis.CSharp;

namespace AvroNet;

internal sealed record class AvroModelOptions(
    string Name,
    string Schema,
    string Namespace,
    string AccessModifier,
    string DeclarationType,
    LanguageVersion LanguageVersion)
{
    public bool UseNullableReferenceTypes { get => LanguageVersion >= LanguageVersion.CSharp8; }
    public bool UseFileScopedNamespaces { get => LanguageVersion >= LanguageVersion.CSharp10; }
    public bool UseRequiredProperties { get => LanguageVersion >= LanguageVersion.CSharp11; }
    public bool UseInitOnlyProperties { get => LanguageVersion >= LanguageVersion.CSharp12; }
    public bool UseUnsafeAccessors { get => LanguageVersion >= LanguageVersion.CSharp12; }
}
