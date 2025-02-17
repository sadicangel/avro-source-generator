namespace AvroSourceGenerator.Parsing;
public readonly record struct GeneratorSettings(
    string? AccessModifier,
    string? RecordDeclaration,
    LanguageFeatures? LanguageFeatures);
