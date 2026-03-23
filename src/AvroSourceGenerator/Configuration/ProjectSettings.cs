using AvroSourceGenerator.Templating;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AvroSourceGenerator.Configuration;

internal readonly record struct ProjectSettings(
    AvroLibrary? AvroLibrary,
    LanguageFeatures? LanguageFeatures,
    AccessModifier? AccessModifier,
    string? RecordDeclaration,
    DuplicateResolution? DuplicateResolution
)
{
    public static ProjectSettings FromOptions(AnalyzerConfigOptionsProvider provider, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var avroLibrary = default(AvroLibrary?);
        if (provider.GlobalOptions.TryGetValue(
                "build_property.AvroSourceGeneratorAvroLibrary",
                out var avroLibraryString) &&
            Enum.TryParse<AvroLibrary>(avroLibraryString, ignoreCase: true, out var parsedAvroLibrary))
        {
            avroLibrary = parsedAvroLibrary;
        }

        var languageFeatures = default(LanguageFeatures?);
        if (provider.GlobalOptions.TryGetValue(
                "build_property.AvroSourceGeneratorLanguageFeatures",
                out var languageFeaturesString) &&
            Enum.TryParse<LanguageFeatures>(languageFeaturesString, ignoreCase: true, out var parsedLanguageFeatures))
        {
            languageFeatures = parsedLanguageFeatures;
        }

        var accessModifier = default(AccessModifier?);
        if (provider.GlobalOptions.TryGetValue(
                "build_property.AvroSourceGeneratorAccessModifier",
                out var accessModifierString) &&
            Enum.TryParse<AccessModifier>(accessModifierString, ignoreCase: true, out var parsedAccessModifier))
        {
            accessModifier = parsedAccessModifier;
        }

        if (!provider.GlobalOptions.TryGetValue(
                "build_property.AvroSourceGeneratorRecordDeclaration",
                out var recordDeclaration) ||
            recordDeclaration is not ("record" or "class"))
        {
            recordDeclaration = null;
        }

        var duplicateResolution = default(DuplicateResolution?);
        if (provider.GlobalOptions.TryGetValue(
                "build_property.AvroSourceGeneratorDuplicateResolution",
                out var duplicateResolutionString) &&
            Enum.TryParse<DuplicateResolution>(duplicateResolutionString, ignoreCase: true, out var parsedDuplicateResolution))
        {
            duplicateResolution = parsedDuplicateResolution;
        }

        return new ProjectSettings(avroLibrary, languageFeatures, accessModifier, recordDeclaration, duplicateResolution);
    }
}
