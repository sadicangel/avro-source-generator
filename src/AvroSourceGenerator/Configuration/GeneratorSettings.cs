namespace AvroSourceGenerator.Configuration;

internal readonly record struct GeneratorSettings(
    AvroLibrary? AvroLibrary,
    LanguageFeatures? LanguageFeatures,
    string? AccessModifier,
    string? RecordDeclaration);
