using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Parsing;

internal sealed record class AvroOptions(
    QualifiedName Name,
    string AccessModifier,
    string RecordDeclaration,
    LanguageFeatures? LanguageFeatures,
    Location Location);
