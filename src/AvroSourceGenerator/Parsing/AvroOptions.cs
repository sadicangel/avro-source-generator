using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Parsing;

internal sealed record class AvroOptions(
    string Name,
    string? Namespace,
    AvroLibrary? AvroLibrary,
    LanguageFeatures? LanguageFeatures,
    string AccessModifier,
    string RecordDeclaration,
    Location Location);
