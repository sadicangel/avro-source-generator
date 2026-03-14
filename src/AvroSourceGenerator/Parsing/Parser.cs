using System.Collections.Immutable;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AvroSourceGenerator.Parsing;

internal static class Parser
{
    public static GeneratorSettings GetGeneratorSettings(
        AnalyzerConfigOptionsProvider provider,
        CancellationToken cancellationToken)
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

        if (!provider.GlobalOptions.TryGetValue(
                "build_property.AvroSourceGeneratorAccessModifier",
                out var accessModifier) ||
            accessModifier is not ("public" or "internal"))
        {
            accessModifier = null;
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

        return new GeneratorSettings(avroLibrary, languageFeatures, accessModifier, recordDeclaration, duplicateResolution);
    }

    public static CompilationInfo GetCompilationInfo(Compilation compilation, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var csharpCompilation = (CSharpCompilation)compilation;

        var avroLibraries = ImmutableArray.CreateBuilder<AvroLibraryReference>();

        if (csharpCompilation.GetTypeByMetadataName("Avro.Specific.ISpecificRecord") is not null)
            avroLibraries.Add(AvroLibraryReference.Apache);

        if (csharpCompilation.GetTypeByMetadataName("Chr.Avro.Abstract.Schema") is not null)
            avroLibraries.Add(AvroLibraryReference.Chr);

        return new CompilationInfo(avroLibraries.ToImmutable(), csharpCompilation.LanguageVersion);
    }

    public static RenderSettings GetRenderSettings((GeneratorSettings generatorSettings, CompilationInfo compilationInfo) input, CancellationToken cancellationToken)
    {
        var (generatorSettings, compilationInfo) = input;

        var diagnostics = ImmutableArray<DiagnosticInfo>.Empty;

        var avroLibrary = generatorSettings.AvroLibrary ?? AvroLibrary.Auto;
        if (avroLibrary is AvroLibrary.Auto)
        {
            avroLibrary = GetAvroLibrary(compilationInfo.AvroLibraries, out diagnostics);
        }

        var languageFeatures = generatorSettings.LanguageFeatures ?? MapVersionToFeatures(compilationInfo.LanguageVersion);
        var accessModifier = generatorSettings.AccessModifier ?? "public";
        var declaration = GetDeclaration(avroLibrary, generatorSettings.RecordDeclaration, languageFeatures);
        var duplicateResolution = generatorSettings.DuplicateResolution ?? DuplicateResolution.Error;

        return new RenderSettings(avroLibrary, compilationInfo.LanguageVersion, languageFeatures, accessModifier, declaration, duplicateResolution, diagnostics);

        static AvroLibrary GetAvroLibrary(ImmutableArray<AvroLibraryReference> references, out ImmutableArray<DiagnosticInfo> diagnostics)
        {
            switch (references)
            {
                case [var reference]:
                    diagnostics = [];
                    return reference.ToAvroLibrary();

                case []:
                    diagnostics = [NoAvroLibraryDetectedDiagnostic.Create(LocationInfo.None)];
                    return AvroLibrary.None;

                default:
                    diagnostics = [MultipleAvroLibrariesDetectedDiagnostic.Create(LocationInfo.None, references)];
                    return AvroLibrary.None;
            }
        }

        static LanguageFeatures MapVersionToFeatures(LanguageVersion languageVersion)
        {
            return languageVersion switch
            {
                <= LanguageVersion.CSharp7_3 => LanguageFeatures.CSharp7_3,
                LanguageVersion.CSharp8 => LanguageFeatures.CSharp8,
                LanguageVersion.CSharp9 => LanguageFeatures.CSharp9,
                LanguageVersion.CSharp10 => LanguageFeatures.CSharp10,
                LanguageVersion.CSharp11 => LanguageFeatures.CSharp11,
                LanguageVersion.CSharp12 => LanguageFeatures.CSharp12,
                //LanguageVersion.CSharp13 => LanguageFeatures.CSharp13,
                //LanguageVersion.CSharp14 => LanguageFeatures.CSharp14,
                _ => LanguageFeatures.Latest,
            };
        }

        static Declaration GetDeclaration(AvroLibrary library, string? recordDeclaration, LanguageFeatures languageFeatures)
        {
            var useRecords = recordDeclaration switch
            {
                "record" => true,
                "class" => false,
                _ => languageFeatures.HasFlag(LanguageFeatures.Records)
            };

            return (library, useRecords) switch
            {
                (AvroLibrary.Apache, true) => Declaration.ApacheRecords,
                (AvroLibrary.Apache, false) => Declaration.ApacheClasses,
                (_, true) => Declaration.Records,
                (_, false) => Declaration.Classes,
            };
        }
    }
}
