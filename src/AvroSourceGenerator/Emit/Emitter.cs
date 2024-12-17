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

            //if (rootSchema.Name != model.ContainingClassName)
            //{
            //    context.ReportDiagnostic(InvalidNameDiagnostic.Create(model.SchemaLocation, rootSchema.Name, model.ContainingClassName));
            //    return;
            //}

            //if (model.NamespaceOverride is null && !string.IsNullOrWhiteSpace(rootSchema.Namespace) && rootSchema.Namespace != model.ContainingNamespace)
            //{
            //    context.ReportDiagnostic(InvalidNamespaceDiagnostic.Create(model.SchemaLocation, rootSchema.Namespace!, model.ContainingNamespace));
            //    return;
            //}

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
}
