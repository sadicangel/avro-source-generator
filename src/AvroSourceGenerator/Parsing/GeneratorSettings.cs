namespace AvroSourceGenerator.Parsing;

internal readonly record struct GeneratorSettings(
    string? AccessModifier,
    string? RecordDeclaration,
    LanguageFeatures? LanguageFeatures);
