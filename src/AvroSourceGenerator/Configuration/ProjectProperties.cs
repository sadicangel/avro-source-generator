using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Templating;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AvroSourceGenerator.Configuration;

internal sealed record ProjectProperties(
    AvroLibrary AvroLibrary,
    LanguageFeatures LanguageFeatures,
    AccessModifier AccessModifier,
    string RecordDeclaration,
    ReferenceResolution ReferenceResolution,
    DuplicateResolution DuplicateResolution,
    PreviewFeatures PreviewFeatures)
{
    public static ProjectProperties FromAnalyzerOptions(AnalyzerConfigOptionsProvider provider, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var avroLibrary = AvroLibrary.Auto;
        if (provider.GlobalOptions.TryGetValue(
                "build_property.AvroSourceGeneratorAvroLibrary",
                out var avroLibraryString) &&
            Enum.TryParse<AvroLibrary>(avroLibraryString, ignoreCase: true, out var parsedAvroLibrary))
        {
            avroLibrary = parsedAvroLibrary;
        }

        var languageFeatures = LanguageFeatures.All;
        if (provider.GlobalOptions.TryGetValue(
                "build_property.AvroSourceGeneratorLanguageFeatures",
                out var languageFeaturesString) &&
            Enum.TryParse<LanguageFeatures>(languageFeaturesString, ignoreCase: true, out var parsedLanguageFeatures))
        {
            languageFeatures = parsedLanguageFeatures;
        }

        var previewFeatures = PreviewFeatures.None;
        if (provider.GlobalOptions.TryGetValue(
                "build_property.AvroSourceGeneratorPreviewFeatures",
                out var previewFeaturesString) &&
            Enum.TryParse<PreviewFeatures>(previewFeaturesString, ignoreCase: true, out var parsedPreviewFeatures))
        {
            previewFeatures = parsedPreviewFeatures;
        }

        var accessModifier = AccessModifier.Public;
        if (provider.GlobalOptions.TryGetValue(
                "build_property.AvroSourceGeneratorAccessModifier",
                out var accessModifierString) &&
            Enum.TryParse<AccessModifier>(accessModifierString, ignoreCase: true, out var parsedAccessModifier))
        {
            accessModifier = parsedAccessModifier;
        }

        var recordDeclaration = "record";
        if (provider.GlobalOptions.TryGetValue(
                "build_property.AvroSourceGeneratorRecordDeclaration",
                out var recordDeclarationString) &&
            recordDeclarationString is "record" or "class")
        {
            recordDeclaration = recordDeclarationString;
        }

        var referenceResolution = ReferenceResolution.Strict;
        if (provider.GlobalOptions.TryGetValue(
                "build_property.AvroSourceGeneratorReferenceResolution",
                out var referenceResolutionString) &&
            Enum.TryParse<ReferenceResolution>(referenceResolutionString, ignoreCase: true, out var parsedReferenceResolution))
        {
            referenceResolution = parsedReferenceResolution;
        }

        var duplicateResolution = DuplicateResolution.Error;
        if (provider.GlobalOptions.TryGetValue(
                "build_property.AvroSourceGeneratorDuplicateResolution",
                out var duplicateResolutionString) &&
            Enum.TryParse<DuplicateResolution>(duplicateResolutionString, ignoreCase: true, out var parsedDuplicateResolution))
        {
            duplicateResolution = parsedDuplicateResolution;
        }

        return new ProjectProperties(avroLibrary, languageFeatures, accessModifier, recordDeclaration, referenceResolution, duplicateResolution, previewFeatures);
    }
}
