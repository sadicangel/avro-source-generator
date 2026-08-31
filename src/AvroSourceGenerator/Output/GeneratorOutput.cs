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

/// <summary>
/// The result of project-wide registration and linking. Rendering intentionally happens in a separate,
/// per-file incremental stage so equivalent files can retain their previous rendered output.
/// </summary>
internal readonly struct GeneratorOutput : IEquatable<GeneratorOutput>
{
    private readonly ImmutableDictionary<string, string?> _fileContents;
    private readonly ImmutableDictionary<SchemaName, string> _ownerPaths;
    private readonly ImmutableArray<FileContent> _projectFingerprint;

    private GeneratorOutput(
        ImmutableArray<TopLevelSchema> schemas,
        ImmutableDictionary<SchemaName, TopLevelSchema> registeredSchemas,
        ImmutableArray<DiagnosticInfo> diagnostics,
        SchemaProject project,
        TemplateSettings settings,
        bool canRender,
        ImmutableArray<FileContent> projectFingerprint)
    {
        Schemas = schemas;
        RegisteredSchemas = registeredSchemas;
        Diagnostics = diagnostics;
        Project = project;
        Settings = settings;
        CanRender = canRender;
        _projectFingerprint = projectFingerprint;
        _fileContents = projectFingerprint.ToImmutableDictionary(file => file.Path, file => file.Text, StringComparer.Ordinal);
        _ownerPaths = project.Schemas.ToImmutableDictionary(schema => schema.SchemaName, schema => schema.SourcePath);
    }

    // Kept as semantic schemas for the P2 graph tests. Source rendering is now downstream of this value.
    public ImmutableArray<TopLevelSchema> Schemas { get; }
    public ImmutableDictionary<SchemaName, TopLevelSchema> RegisteredSchemas { get; }
    public ImmutableArray<DiagnosticInfo> Diagnostics { get; }
    public SchemaProject Project { get; }
    public TemplateSettings Settings { get; }
    public bool CanRender { get; }

    public bool Equals(GeneratorOutput other) =>
        CanRender == other.CanRender &&
        Settings == other.Settings &&
        Diagnostics.SequenceEqual(other.Diagnostics) &&
        Project == other.Project &&
        _projectFingerprint.SequenceEqual(other._projectFingerprint);

    public override bool Equals(object? obj) => obj is GeneratorOutput other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(CanRender);
        hash.Add(Settings);
        foreach (var diagnostic in Diagnostics)
            hash.Add(diagnostic);
        hash.Add(Project);
        foreach (var file in _projectFingerprint)
            hash.Add(file);

        return hash.ToHashCode();
    }

    public FileRenderInput CreateFileRenderInput(IAvroFile file)
    {
        if (!CanRender)
            return new FileRenderInput(file.Path, [], [], Settings, []);

        var exportedSchemas = new HashSet<SchemaName>(Project.Schemas
            .Where(schema => schema.SourcePath == file.Path && schema.EmitsSource)
            .Select(schema => schema.SchemaName));

        var schemas = Schemas.Where(schema => exportedSchemas.Contains(schema.SchemaName)).ToImmutableArray();
        var ownerPaths = _ownerPaths;
        var fileContents = _fileContents;
        var dependencyClosure = GetDependencyClosure(exportedSchemas).ToImmutableHashSet();
        var registeredSchemas = RegisteredSchemas
            .Where(schema => dependencyClosure.Contains(schema.Key))
            .ToImmutableDictionary();
        var fingerprints = dependencyClosure
            .OrderBy(static schema => schema.FullName, StringComparer.Ordinal)
            .Select(schema => $"{schema.FullName}\0{ownerPaths[schema]}\0{fileContents[ownerPaths[schema]]}")
            .ToImmutableArray();

        return new FileRenderInput(file.Path, schemas, registeredSchemas, Settings, fingerprints);
    }

    public static GeneratorOutput FromInput((ImmutableArray<IAvroFile>, GeneratorConfig) source, CancellationToken cancellationToken)
    {
        var (files, config) = source;
        var diagnostics = config.Diagnostics.AddRange(files.SelectMany(avroFile => avroFile.Diagnostics));
        var settings = new TemplateSettings(config.TargetProfile, config.LanguageFeatures, config.AccessModifier);
        var projectFingerprint = files.Select(file => new FileContent(file.Path, file.Text)).ToImmutableArray();
        if (!config.IsValid)
        {
            return new GeneratorOutput([], [], diagnostics, SchemaProject.Empty, settings, canRender: false, projectFingerprint);
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
            return new GeneratorOutput([], [], diagnostics, project, settings, canRender: false, projectFingerprint);
        }

        var schemas = schemaRegistry.ToImmutableArray();
        var registeredSchemas = schemas.ToImmutableDictionary(schema => schema.SchemaName);
        return new GeneratorOutput(schemas, registeredSchemas, diagnostics, project, settings, canRender: true, projectFingerprint);
    }

    private IEnumerable<SchemaName> GetDependencyClosure(IEnumerable<SchemaName> roots)
    {
        var visited = new HashSet<SchemaName>();
        var pending = new Stack<SchemaName>(roots);

        while (pending.Count > 0)
        {
            var schema = pending.Pop();
            if (!visited.Add(schema))
                continue;

            if (Project.ForwardDependencies.TryGetValue(schema, out var dependencies))
            {
                foreach (var dependency in dependencies)
                    pending.Push(dependency);
            }
        }

        return visited;
    }

    private readonly record struct FileContent(string Path, string? Text);
}
