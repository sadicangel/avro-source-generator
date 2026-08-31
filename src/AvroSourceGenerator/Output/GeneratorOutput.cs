using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Avdl;
using AvroSourceGenerator.Avsc;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Exceptions;
using AvroSourceGenerator.Inputs;
using AvroSourceGenerator.Registry;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Templating;

namespace AvroSourceGenerator.Output;

internal readonly record struct GeneratorOutput(
    ImmutableArray<RenderedSchema> Schemas,
    ImmutableArray<DiagnosticInfo> Diagnostics,
    SchemaProject Project)
{
    public bool Equals(GeneratorOutput other)
    {
        var (schemasX, diagnosticsX, projectX) = this;
        var (schemasY, diagnosticsY, projectY) = other;
        return schemasX.SequenceEqual(schemasY) && diagnosticsX.SequenceEqual(diagnosticsY) && projectX == projectY;
    }

    public override int GetHashCode()
    {
        var (schemas, diagnostics, project) = this;
        var hash = new HashCode();
        foreach (var schema in schemas)
        {
            hash.Add(schema);
        }

        foreach (var diagnostic in diagnostics)
        {
            hash.Add(diagnostic);
        }

        hash.Add(project);

        return hash.ToHashCode();
    }

    public static GeneratorOutput FromInput((ImmutableArray<IAvroFile>, GeneratorConfig) source, CancellationToken cancellationToken)
    {
        var (files, config) = source;
        var diagnostics = config.Diagnostics.AddRange(files.SelectMany(avroFile => avroFile.Diagnostics));
        if (!config.IsValid)
        {
            return new GeneratorOutput([], diagnostics, SchemaProject.Empty);
        }

        var schemaRegistry = new SchemaRegistry(
            new SchemaRegistryOptions(
                TargetProfile: config.TargetProfile,
                UseNullableReferenceTypes: config.LanguageFeatures.HasFlag(LanguageFeatures.NullableReferenceTypes),
                ReferenceResolution: config.ReferenceResolution,
                DuplicateResolution: config.DuplicateResolution));
        var projectBuilder = new SchemaProjectBuilder();

        foreach (var file in files)
        {
            var registrationStart = schemaRegistry.Registrations.Count;
            var strictMissingReferences = ImmutableArray<SchemaName>.Empty;
            try
            {
                switch (file)
                {
                    case AvroInvalidFile:
                    case { IsValid: false }:
                        break;

                    case AvroSchemaFile schemaFile:
                        schemaRegistry.RegisterSchema(schema: schemaFile.Json);
                        break;

                    case AvroSourceFile sourceFile:
                        schemaRegistry.RegisterSource(syntaxTree: sourceFile.SyntaxTree);
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
                strictMissingReferences = ex.MissingReferences;
                diagnostics = diagnostics.Add(MissingReferenceDiagnostic.Create(LocationInfo.FromSourceFile(file.Path, file.Text), ex.MissingReferences));
            }
            catch (InvalidSourceException ex)
            {
                diagnostics = diagnostics.Add(InvalidSyntaxDiagnostic.Create(LocationInfo.FromSourceSpan(ex.SourceSpan), ex.Message));
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
            finally
            {
                projectBuilder.AddFile(
                    file.Path,
                    schemaRegistry.Registrations.Skip(registrationStart).ToArray(),
                    strictMissingReferences);
            }
        }

        var project = projectBuilder.Build(in schemaRegistry);

        var missingReferences = schemaRegistry.GetMissingReferences();
        if (!missingReferences.IsEmpty)
        {
            diagnostics = diagnostics.Add(MissingReferenceDiagnostic.Create(LocationInfo.None, missingReferences));
            return new GeneratorOutput([], diagnostics, project);
        }

        var schemas = AvroTemplate.Render(in schemaRegistry, new TemplateSettings(config.TargetProfile, config.LanguageFeatures, config.AccessModifier));

        return new GeneratorOutput(schemas, diagnostics, project);
    }
}
