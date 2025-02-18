using System.Text;
using System.Text.Json;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Parsing;
using AvroSourceGenerator.Schemas;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace AvroSourceGenerator.Emit;

internal static class Emitter
{
    public static void Emit(SourceProductionContext context, (AvroFile avroFile, GeneratorSettings generatorSettings, CompilationInfo compilationInfo, AvroOptions? avroOptions) source)
    {
        var (avroFile, generatorSettings, compilationInfo, avroOptions) = source;

        foreach (var diagnostic in avroFile.Diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }

        if (!avroFile.IsValid)
        {
            return;
        }

        try
        {
            var languageFeatures = avroOptions?.LanguageFeatures
                ?? generatorSettings.LanguageFeatures
                ?? MapVersionToFeatures(compilationInfo.LanguageVersion);

            var accessModifier = avroOptions?.AccessModifier
                ?? generatorSettings.AccessModifier
                ?? "public";

            var recordDeclaration = avroOptions?.RecordDeclaration
                ?? generatorSettings.RecordDeclaration
                ?? (languageFeatures.HasFlag(LanguageFeatures.Records) ? "record" : "class");

            var schemaRegistry = new SchemaRegistry(languageFeatures.HasFlag(LanguageFeatures.NullableReferenceTypes));
            var rootSchema = schemaRegistry.Register(avroFile.Json);

            // We should get no render errors, so we don't have to handle anything else.
            var renderOutputs = AvroTemplate.Render(
                schemaRegistry,
                languageFeatures,
                accessModifier,
                recordDeclaration);

            foreach (var renderOutput in renderOutputs)
            {
                context.AddSource(renderOutput.HintName, SourceText.From(renderOutput.SourceText, Encoding.UTF8));
            }
        }
        catch (JsonException ex)
        {
            // TODO: We can probably get a better location for the error.
            context.ReportDiagnostic(InvalidJsonDiagnostic.Create(avroFile.GetLocation(), ex.Message));
        }
        catch (InvalidSchemaException ex)
        {
            // TODO: We can probably get a better location for the error.
            context.ReportDiagnostic(InvalidSchemaDiagnostic.Create(avroFile.GetLocation(), ex.Message));
        }
    }

    private static LanguageFeatures MapVersionToFeatures(LanguageVersion languageVersion)
    {
        return languageVersion switch
        {
            <= LanguageVersion.CSharp7_3 => LanguageFeatures.CSharp7_3,
            LanguageVersion.CSharp8 => LanguageFeatures.CSharp8,
            LanguageVersion.CSharp9 => LanguageFeatures.CSharp9,
            LanguageVersion.CSharp10 => LanguageFeatures.CSharp10,
            LanguageVersion.CSharp11 => LanguageFeatures.CSharp11,
            LanguageVersion.CSharp12 => LanguageFeatures.CSharp12,
            LanguageVersion.CSharp13 => LanguageFeatures.CSharp13,
            _ => LanguageFeatures.Latest,
        };
    }
}
