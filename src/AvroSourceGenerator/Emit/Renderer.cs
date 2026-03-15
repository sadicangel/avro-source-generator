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
    public static RenderResult Render((ImmutableArray<IAvroFile> avroFiles, RenderSettings renderSettings) source, CancellationToken cancellationToken)
    {
        var (avroFiles, settings) = source;

        var diagnostics = settings.Diagnostics.AddRange(avroFiles.SelectMany(avroFile => avroFile.Diagnostics));

        if (!settings.IsValid)
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
                switch (avroFile)
                {
                    case AvroSchemaFile schemaFile:
                        schemaRegistry.RegisterSchema(schema: schemaFile.Json);
                        break;

                    case AvroSubjectFile subjectFile:
                        schemaRegistry.RegisterSubject(subject: subjectFile.Json);
                        break;

                    case AvroInvalidFile:
                        break;

                    default:
                        // If we get here, it means we've forgotten to handle a new IAvroFile type. This
                        // should never happen, but if it does, we want to know about it so we can fix the code.
                        throw new InvalidOperationException($"Unhandled IAvroFile type: {avroFile.GetType()}");
                }
            }
            catch (JsonException ex)
            {
                diagnostics = diagnostics.Add(InvalidJsonDiagnostic.Create(LocationInfo.FromException(avroFile.Path, avroFile.Text, ex), ex.Message));
            }
            catch (DuplicateSchemaException ex)
            {
                diagnostics = diagnostics.Add(DuplicateSchemaDiagnostic.Create(LocationInfo.None, ex.Schema.CSharpName.ToString(includeGlobalPrefix: false)));
            }
            catch (MissingReferenceException ex)
            {
                diagnostics = diagnostics.Add(MissingReferenceDiagnostic.Create(LocationInfo.FromSourceFile(avroFile.Path, avroFile.Text), ex.MissingReferences));
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

        var missingReferences = schemaRegistry.GetMissingReferences();
        if (missingReferences.Length > 0)
        {
            diagnostics = diagnostics.Add(MissingReferenceDiagnostic.Create(LocationInfo.None, missingReferences));
            return new RenderResult([], diagnostics);
        }

        var renderedSchemas = AvroTemplate.Render(schemaRegistry, settings);

        return new RenderResult(renderedSchemas, diagnostics);
    }
}
