using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Registry.Extensions;
using AvroSourceGenerator.Schemas;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AvroSourceGenerator.Parsing;

internal static class Parser
{
    public static bool IsAvroFile(AdditionalText text) =>
        text.Path.EndsWith(".avsc", StringComparison.OrdinalIgnoreCase);

    public static AvroFile GetAvroFile(AdditionalText additionalText, CancellationToken cancellationToken)
    {
        var path = additionalText.Path;
        var text = additionalText.GetText(cancellationToken)?.ToString();
        var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();
        if (string.IsNullOrWhiteSpace(text))
        {
            diagnostics.Add(InvalidJsonDiagnostic.Create(LocationInfo.FromSourceFile(path, text), "The file is empty."));

            return new AvroFile(path, text, default, default, diagnostics.ToImmutable());
        }

        try
        {
            using var jsonDocument = JsonDocument.Parse(text!);
            var json = jsonDocument.RootElement.Clone();
            var name = json.GetOptionalSchemaName();
            return new AvroFile(path, text, json, name, diagnostics.ToImmutable());
        }
        catch (JsonException ex)
        {
            diagnostics.Add(InvalidJsonDiagnostic.Create(LocationInfo.FromException(path, text, ex), ex.Message));
        }
        catch (InvalidSchemaException ex)
        {
            diagnostics.Add(InvalidSchemaDiagnostic.Create(LocationInfo.FromSourceFile(path, text), ex.Message));
        }

        return new AvroFile(path, text, default, default, diagnostics.ToImmutable());
    }

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

        return new GeneratorSettings(avroLibrary, languageFeatures, accessModifier, recordDeclaration);
    }

    public static CompilationInfo GetCompilationInfo(Compilation compilation, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var csharpCompilation = (CSharpCompilation)compilation;

        var avroLibraries = ImmutableArray.CreateBuilder<AvroLibraryReference>();

        if (csharpCompilation.GetTypeByMetadataName("Avro.Specific.ISpecificRecord") is not null)
            avroLibraries.Add(AvroLibraryReference.Apache);

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
        var recordDeclaration = generatorSettings.RecordDeclaration ?? (languageFeatures.HasFlag(LanguageFeatures.Records) ? "record" : "class");

        return new RenderSettings(avroLibrary, compilationInfo.LanguageVersion, languageFeatures, accessModifier, recordDeclaration, diagnostics);

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
    }
}
