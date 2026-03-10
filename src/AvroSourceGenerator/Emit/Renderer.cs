using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Exceptions;
using AvroSourceGenerator.Parsing;
using AvroSourceGenerator.Registry;

namespace AvroSourceGenerator.Emit;

internal static class Renderer
{
    public static RenderResult Render((ImmutableArray<AvroFile> avroFiles, RenderSettings renderSettings) source, CancellationToken cancellationToken)
    {
        var (avroFiles, settings) = source;

        var diagnostics = settings.Diagnostics.AddRange(avroFiles.SelectMany(avroFile => avroFile.Diagnostics));

        // TODO: We can probably skip invalid files and just render the valid ones, but for now we'll just return an empty result if there are any errors.
        if (!settings.IsValid || avroFiles.Any(avroFile => !avroFile.IsValid))
        {
            return new RenderResult([], diagnostics);
        }

        var schemaRegistry = new SchemaRegistry(
            new SchemaRegistryOptions(
                TargetProfile: settings.TargetProfile,
                UseNullableReferenceTypes: settings.LanguageFeatures.HasFlag(LanguageFeatures.NullableReferenceTypes),
                DuplicateResolution: settings.DuplicateResolution));

        foreach (var avroFile in avroFiles)
        {
            try
            {
                schemaRegistry.Register(schema: avroFile.Json);
            }
            catch (JsonException ex)
            {
                diagnostics = diagnostics.Add(InvalidJsonDiagnostic.Create(LocationInfo.FromException(avroFile.Path, avroFile.Text, ex), ex.Message));
            }
            catch (DuplicateSchemaException ex)
            {
                diagnostics = diagnostics.Add(DuplicateSchemaOutputDiagnostic.Create(LocationInfo.None, $"{ex.Schema.SchemaName}.Avro.g.cs"));
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
        }

        var renderedSchemas = AvroTemplate.Render(schemaRegistry, settings);

        return new RenderResult(renderedSchemas, diagnostics);
    }
}
