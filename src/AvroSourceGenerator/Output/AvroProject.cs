using System.Collections.Immutable;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Inputs;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Templating;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Output;

internal sealed class AvroProject : IEquatable<AvroProject>
{
    private readonly ImmutableArray<BoundAvroFile> _files;
    private readonly AvroProjectOptions _options;
    private readonly ImmutableDictionary<SchemaName, TopLevelSchema> _schemas;
    private readonly ImmutableDictionary<SchemaName, BoundAvroFile> _owners;
    private readonly ImmutableDictionary<SchemaName, ImmutableArray<SchemaName>> _dependencies;

    private AvroProject(
        ImmutableArray<BoundAvroFile> files,
        AvroProjectOptions options,
        ImmutableDictionary<SchemaName, TopLevelSchema> schemas,
        ImmutableDictionary<SchemaName, BoundAvroFile> owners,
        ImmutableDictionary<SchemaName, ImmutableArray<SchemaName>> dependencies,
        ImmutableArray<DiagnosticInfo> diagnostics,
        bool canRender)
    {
        _files = files;
        _options = options;
        _schemas = schemas;
        _owners = owners;
        _dependencies = dependencies;
        Diagnostics = diagnostics;
        CanRender = canRender;
    }

    public ImmutableArray<DiagnosticInfo> Diagnostics { get; }

    public bool CanRender { get; }

    public bool Equals(AvroProject? other) =>
        ReferenceEquals(this, other) ||
        other is not null &&
        _options == other._options &&
        _files.SequenceEqual(other._files);

    public override bool Equals(object? obj) => obj is AvroProject other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_options);
        foreach (var file in _files)
            hash.Add(file);
        return hash.ToHashCode();
    }

    public static AvroProject FromInput((ImmutableArray<BoundAvroFile>, AvroProjectOptions) input, CancellationToken cancellationToken)
    {
        var (files, options) = input;
        var diagnostics = options.Diagnostics
            .AddRange(files.SelectMany(static file => file.File.Diagnostics));
        var importDiagnostics = GetUnsupportedImportDiagnostics(files, options.ReferenceResolution);
        diagnostics = diagnostics.AddRange(importDiagnostics);

        if (!options.IsValid || !importDiagnostics.IsEmpty)
        {
            return new AvroProject(
                files,
                options,
                [],
                [],
                [],
                diagnostics,
                canRender: false);
        }

        var schemas = new Dictionary<SchemaName, TopLevelSchema>();
        var owners = new Dictionary<SchemaName, BoundAvroFile>();
        var dependencies = new Dictionary<SchemaName, ImmutableArray<SchemaName>>();
        var localNames = new HashSet<SchemaName>();
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            localNames.Clear();
            foreach (var declaration in file.Declarations)
            {
                var name = declaration.SchemaName;
                if (!localNames.Add(name))
                {
                    diagnostics = diagnostics.Add(DuplicateDiagnostic(declaration));
                    continue;
                }

                if (schemas.ContainsKey(name))
                {
                    if (options.DuplicateResolution is DuplicateResolution.Error)
                        diagnostics = diagnostics.Add(DuplicateDiagnostic(declaration));
                    continue;
                }

                schemas.Add(name, declaration);
                owners.Add(name, file);
                dependencies.Add(
                    name,
                    file.Dependencies.TryGetValue(name, out var schemaDependencies)
                        ? schemaDependencies
                        : []);
            }
        }

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var missingReferences = file.References.Keys
                .Where(reference =>
                    options.ReferenceResolution is ReferenceResolution.Strict ||
                    !schemas.ContainsKey(reference))
                .OrderBy(static reference => reference.FullName, StringComparer.Ordinal)
                .ToImmutableArray();
            if (!missingReferences.IsEmpty)
            {
                diagnostics = diagnostics.Add(MissingReferenceDiagnostic.Create(
                    LocationInfo.FromSourceFile(file.File.SourceText.Path, file.File.SourceText.Text),
                    missingReferences));
            }
        }

        var hasErrors = diagnostics.Any(static diagnostic =>
            diagnostic.Descriptor.DefaultSeverity is DiagnosticSeverity.Error);
        return new AvroProject(
            files,
            options,
            schemas.ToImmutableDictionary(),
            owners.ToImmutableDictionary(),
            dependencies.ToImmutableDictionary(),
            diagnostics,
            canRender: !hasErrors);
    }

    public RenderableAvroFile CreateRenderableFile(BoundAvroFile file)
    {
        if (!CanRender)
            return RenderableAvroFile.Invalid();

        var emittedSchemas = file.Declarations
            .Where(declaration =>
                _owners.TryGetValue(declaration.SchemaName, out var owner) &&
                ReferenceEquals(owner, file) &&
                EmitsSource(declaration))
            .ToImmutableArray();
        var closure = GetDependencyClosure(emittedSchemas.Select(static schema => schema.SchemaName));
        var renderOptions = new RenderOptions(
            _options.TargetProfile,
            _options.LanguageFeatures,
            _options.AccessModifier);
        var contributingFiles = closure
            .Select(name => _owners[name])
            .Distinct()
            .OrderBy(static owner => owner.File.SourceText.Path, StringComparer.Ordinal)
            .ToImmutableArray();

        return new RenderableAvroFile(
            emittedSchemas,
            _schemas,
            contributingFiles,
            renderOptions);
    }

    private ImmutableArray<SchemaName> GetDependencyClosure(IEnumerable<SchemaName> roots)
    {
        var visited = new HashSet<SchemaName>();
        var pending = new Stack<SchemaName>(roots);
        while (pending.Count > 0)
        {
            var schema = pending.Pop();
            if (!visited.Add(schema))
                continue;

            if (!_dependencies.TryGetValue(schema, out var dependencies))
                continue;
            for (var index = dependencies.Length - 1; index >= 0; index--)
            {
                var dependency = dependencies[index];
                if (_schemas.ContainsKey(dependency))
                    pending.Push(dependency);
            }
        }

        return [.. visited.OrderBy(static name => name.FullName, StringComparer.Ordinal)];
    }

    private static DiagnosticInfo DuplicateDiagnostic(TopLevelSchema declaration) =>
        DuplicateSchemaDiagnostic.Create(
            LocationInfo.None,
            declaration.CSharpName.ToString(includeGlobalPrefix: false));

    private static ImmutableArray<DiagnosticInfo> GetUnsupportedImportDiagnostics(
        ImmutableArray<BoundAvroFile> files,
        ReferenceResolution referenceResolution)
    {
        if (referenceResolution is not ReferenceResolution.Strict)
            return [];

        ImmutableArray<DiagnosticInfo>.Builder? diagnostics = null;
        foreach (var file in files)
        {
            if (file.File.Imports.IsEmpty)
                continue;

            const string UnsupportedImportMessage =
               "Imports are not yet supported in Avro IDL files. To work around this limitation, set " +
               "AvroSourceGeneratorReferenceResolution to Deferred and include imported files as AdditionalFiles.";

            diagnostics ??= ImmutableArray.CreateBuilder<DiagnosticInfo>();
            diagnostics.Add(InvalidSyntaxDiagnostic.Create(
                LocationInfo.FromSourceFile(file.File.SourceText.Path, file.File.SourceText.Text),
                UnsupportedImportMessage));
        }

        return diagnostics?.ToImmutable() ?? [];
    }

    private static bool EmitsSource(TopLevelSchema schema) =>
        schema.Type is not SchemaType.Fixed ||
        schema.CSharpName != AvroSchema.Bytes.CSharpName;
}
