using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Parsing;

internal sealed record class AvroOptions(
    string Name,
    string? Namespace,
    string AccessModifier,
    string RecordDeclaration,
    LanguageFeatures? LanguageFeatures,
    Location Location);
