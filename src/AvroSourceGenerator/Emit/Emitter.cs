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
    public static void Emit(SourceProductionContext context, (AvroModel avroModel, LanguageVersion languageVersion) source)
    {
        var (avroModel, languageVersion) = source;

        foreach (var diagnostic in avroModel.Diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }

        if (avroModel.SchemaJson is null)
        {
            return;
        }

        try
        {
            var languageFeatures = MapSpecifiedToEffectiveFeatures(avroModel.LanguageFeatures, languageVersion);

            using var document = JsonDocument.Parse(avroModel.SchemaJson);
            var schemaRegistry = new SchemaRegistry(languageFeatures, avroModel.NamespaceOverride);
            var rootSchema = schemaRegistry.Register(document.RootElement, avroModel.ContainingNamespace);

            if (!NameMatches(rootSchema.Name, avroModel.ContainingClassName))
            {
                context.ReportDiagnostic(InvalidNameDiagnostic.Create(avroModel.SchemaLocation, rootSchema.Name, avroModel.ContainingClassName));
                return;
            }

            if (avroModel.NamespaceOverride is null && !NamespaceMatches(rootSchema.Namespace, avroModel.ContainingNamespace))
            {
                context.ReportDiagnostic(InvalidNamespaceDiagnostic.Create(avroModel.SchemaLocation, rootSchema.Namespace!, avroModel.ContainingNamespace));
                return;
            }

            // We should get no render errors, so we don't have to handle anything else.
            var renderOutputs = AvroTemplate.Render(
                schemaRegistry,
                languageFeatures,
                avroModel.RecordDeclaration,
                avroModel.AccessModifier);

            foreach (var renderOutput in renderOutputs)
            {
                context.AddSource(renderOutput.HintName, SourceText.From(renderOutput.SourceText, Encoding.UTF8));
            }
        }
        catch (JsonException ex)
        {
            context.ReportDiagnostic(InvalidJsonDiagnostic.Create(avroModel.SchemaLocation, ex.Message));
        }
        catch (InvalidSchemaException ex)
        {
            context.ReportDiagnostic(InvalidAvroSchemaDiagnostic.Create(avroModel.SchemaLocation, ex.Message));
        }
    }

    private static LanguageFeatures MapSpecifiedToEffectiveFeatures(LanguageFeatures? languageFeatures, LanguageVersion languageVersion)
    {
        return languageFeatures ?? languageVersion switch
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

    private static bool NameMatches(string schemaName, string className) =>
        schemaName == className || schemaName.AsSpan(1).Equals(className.AsSpan(), StringComparison.Ordinal);

    private static bool NamespaceMatches(string? schemaNamespace, string classNamespace) =>
        string.IsNullOrWhiteSpace(schemaNamespace) || schemaNamespace == classNamespace || schemaNamespace.AsSpan(1).Equals(classNamespace.AsSpan(), StringComparison.Ordinal);
}
