using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator;

internal readonly record struct SchemaFieldInfo(string SchemaJson, Location Location)
{
    public bool IsValid => SchemaJson is not null && Location is not null;
}

internal sealed record class SourceOutputModel(
    LanguageFeatures LanguageFeatures,
    string? NamespaceOverride,
    string RecordDeclaration,
    string AccessModifier,
    SchemaFieldInfo SchemaField,
    ImmutableArray<Diagnostic> Diagnostics);
