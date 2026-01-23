using System.Text.Json;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Parsing;
using AvroSourceGenerator.Registry;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Emit;

internal static class Renderer
{
    public static RenderResult Render((AvroFile avroFile, RenderSettings renderSettings) source, CancellationToken cancellationToken)
    {
        var (avroFile, settings) = source;

        var diagnostics = avroFile.Diagnostics.AddRange(settings.Diagnostics);

        if (!avroFile.IsValid || !settings.IsValid)
        {
            return new RenderResult([], diagnostics);
        }

        try
        {
            var schemaRegistry = SchemaRegistry.Register(
                schema: avroFile.Json,
                avroLibrary: settings.AvroLibrary,
                languageVersion: settings.LanguageVersion,
                useNullableReferenceTypes: settings.LanguageFeatures.HasFlag(LanguageFeatures.NullableReferenceTypes));

            // We should get no render errors, so we don't have to handle anything else.
            var schemas = AvroTemplate.Render(schemaRegistry, settings);

            return new RenderResult(schemas, diagnostics);
        }
        catch (JsonException ex)
        {
            diagnostics = diagnostics.Add(InvalidJsonDiagnostic.Create(LocationInfo.FromException(avroFile.Path, avroFile.Text, ex), ex.Message));
        }
        catch (InvalidSchemaException ex)
        {
            // TODO: We can probably get a better location for the error.
            diagnostics = diagnostics.Add(InvalidSchemaDiagnostic.Create(LocationInfo.FromSourceFile(avroFile.Path, avroFile.Text), ex.Message));
        }
        catch (Exception ex)
        {
            diagnostics = diagnostics.Add(UnknownErrorDiagnostic.Create(LocationInfo.FromSourceFile(avroFile.Path, avroFile.Text), ex.Message));
        }

        return new RenderResult([], diagnostics);
    }
}
