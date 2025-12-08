using System.Text;
using System.Text.Json;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Parsing;
using AvroSourceGenerator.Registry;
using AvroSourceGenerator.Schemas;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AvroSourceGenerator.Emit;

internal static class Emitter
{
    public static void Emit(SourceProductionContext context, (AvroFile avroFile, RenderSettings renderSettings) source)
    {
        var (avroFile, settings) = source;

        foreach (var diagnostic in avroFile.Diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }

        foreach (var diagnostic in settings.Diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }

        if (!avroFile.IsValid || !settings.IsValid)
        {
            return;
        }

        try
        {
            var schemaRegistry = SchemaRegistry.Register(
                schema: avroFile.Json,
                avroLibrary: settings.AvroLibrary,
                languageVersion: settings.LanguageVersion,
                useNullableReferenceTypes: settings.LanguageFeatures.HasFlag(LanguageFeatures.NullableReferenceTypes));

            // We should get no render errors, so we don't have to handle anything else.
            var renderOutputs = AvroTemplate.Render(schemaRegistry, settings);

            foreach (var renderOutput in renderOutputs)
            {
                context.AddSource(renderOutput.HintName, SourceText.From(renderOutput.SourceText, Encoding.UTF8));
            }
        }
        catch (JsonException ex)
        {
            context.ReportDiagnostic(InvalidJsonDiagnostic.Create(LocationInfo.FromException(avroFile.Path, avroFile.Text, ex), ex.Message));
        }
        catch (InvalidSchemaException ex)
        {
            // TODO: We can probably get a better location for the error.
            context.ReportDiagnostic(InvalidSchemaDiagnostic.Create(LocationInfo.FromSourceFile(avroFile.Path, avroFile.Text), ex.Message));
        }
    }
}
