using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Exceptions;
using AvroSourceGenerator.Inputs;
using AvroSourceGenerator.Registry;
using AvroSourceGenerator.Templating;

namespace AvroSourceGenerator.Output;

internal readonly record struct GeneratorOutput(ImmutableArray<RenderedSchema> Schemas, ImmutableArray<DiagnosticInfo> Diagnostics)
{
    public bool Equals(GeneratorOutput other)
    {
        var (schemasX, diagnosticsX) = this;
        var (schemasY, diagnosticsY) = other;
        return schemasX.SequenceEqual(schemasY) && diagnosticsX.SequenceEqual(diagnosticsY);
    }

    public override int GetHashCode()
    {
        var (schemas, diagnostics) = this;
        var hash = new HashCode();
        foreach (var schema in schemas)
        {
            hash.Add(schema);
        }

        foreach (var diagnostic in diagnostics)
        {
            hash.Add(diagnostic);
        }

        return hash.ToHashCode();
    }

    public static GeneratorOutput FromInput((ImmutableArray<IAvroFile>, GeneratorConfig) source, CancellationToken cancellationToken)
    {
        var (files, config) = source;
        var diagnostics = config.Diagnostics.AddRange(files.SelectMany(avroFile => avroFile.Diagnostics));
        if (!config.IsValid)
        {
            return new GeneratorOutput([], diagnostics);
        }

        var schemaRegistry = new SchemaRegistry(
            new SchemaRegistryOptions(
                TargetProfile: config.TargetProfile,
                UseNullableReferenceTypes: config.LanguageFeatures.HasFlag(LanguageFeatures.NullableReferenceTypes),
                DuplicateResolution: config.DuplicateResolution));

        foreach (var file in files)
        {
            try
            {
                switch (file)
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
                        throw new InvalidOperationException($"Unhandled IAvroFile type: {file.GetType()}");
                }
            }
            catch (JsonException ex)
            {
                diagnostics = diagnostics.Add(InvalidJsonDiagnostic.Create(LocationInfo.FromException(file.Path, file.Text, ex), ex.Message));
            }
            catch (DuplicateSchemaException ex)
            {
                diagnostics = diagnostics.Add(DuplicateSchemaDiagnostic.Create(LocationInfo.None, ex.Schema.CSharpName.ToString(includeGlobalPrefix: false)));
            }
            catch (MissingReferenceException ex)
            {
                diagnostics = diagnostics.Add(MissingReferenceDiagnostic.Create(LocationInfo.FromSourceFile(file.Path, file.Text), ex.MissingReferences));
            }
            catch (InvalidSchemaException ex)
            {
                // TODO: We can probably get a better location for the error.
                diagnostics = diagnostics.Add(InvalidSchemaDiagnostic.Create(LocationInfo.FromSourceFile(file.Path, file.Text), ex.Message));
            }
            catch (Exception ex)
            {
                diagnostics = diagnostics.Add(UnknownErrorDiagnostic.Create(LocationInfo.FromSourceFile(file.Path, file.Text), ex.Message));
            }
        }

        var missingReferences = schemaRegistry.GetMissingReferences();
        if (!missingReferences.IsEmpty)
        {
            diagnostics = diagnostics.Add(MissingReferenceDiagnostic.Create(LocationInfo.None, missingReferences));
            return new GeneratorOutput([], diagnostics);
        }

        var schemas = AvroTemplate.Render(in schemaRegistry, new TemplateSettings(config.TargetProfile, config.LanguageFeatures, config.AccessModifier));

        return new GeneratorOutput(schemas, diagnostics);
    }
}
