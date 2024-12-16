using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator;

internal sealed record class AvroModel(
    LanguageFeatures LanguageFeatures,
    string ContainingClassName,
    string ContainingNamespace,
    string? NamespaceOverride,
    string RecordDeclaration,
    string AccessModifier,
    string? SchemaJson,
    Location SchemaLocation,
    EquatableArray<Diagnostic> Diagnostics);
