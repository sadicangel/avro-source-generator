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

        if (!options.IsValid)
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

        var schemaIndex = BuildSchemaIndex(
            files,
            options.DuplicateResolution,
            cancellationToken);
        diagnostics = diagnostics
            .AddRange(schemaIndex.Diagnostics)
            .AddRange(ValidateReferences(
                files,
                schemaIndex,
                options.ReferenceResolution,
                cancellationToken));

        var hasErrors = diagnostics.Any(static diagnostic =>
            diagnostic.Descriptor.DefaultSeverity is DiagnosticSeverity.Error);
        return new AvroProject(
            files,
            options,
            schemaIndex.Schemas.ToImmutableDictionary(),
            schemaIndex.Owners.ToImmutableDictionary(
                static owner => owner.Key,
                static owner => owner.Value.File),
            schemaIndex.Dependencies.ToImmutableDictionary(),
            diagnostics,
            canRender: !hasErrors);
    }

    private static SchemaIndex BuildSchemaIndex(
        ImmutableArray<BoundAvroFile> files,
        DuplicateResolution duplicateResolution,
        CancellationToken cancellationToken)
    {
        var schemaIndex = new SchemaIndex();
        var localNames = new HashSet<SchemaName>();
        foreach (var (fileIndex, file) in files.Index())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var filePath = file.File.SourceText.Path;
            schemaIndex.FileIndexes.TryAdd(filePath, fileIndex);

            localNames.Clear();
            foreach (var declaration in file.Declarations)
            {
                var name = declaration.SchemaName;
                if (!localNames.Add(name))
                {
                    schemaIndex.Diagnostics.Add(DuplicateDiagnostic(declaration));
                    continue;
                }

                if (schemaIndex.Schemas.ContainsKey(name))
                {
                    if (duplicateResolution is DuplicateResolution.Error)
                        schemaIndex.Diagnostics.Add(DuplicateDiagnostic(declaration));
                    continue;
                }

                schemaIndex.Schemas.Add(name, declaration);
                schemaIndex.Owners.Add(name, new SchemaOwner(file, fileIndex));
                schemaIndex.Dependencies.Add(
                    name,
                    file.Dependencies.TryGetValue(name, out var schemaDependencies)
                        ? schemaDependencies
                        : []);
            }
        }

        return schemaIndex;
    }

    private static ImmutableArray<DiagnosticInfo> ValidateReferences(
        ImmutableArray<BoundAvroFile> files,
        SchemaIndex schemaIndex,
        ReferenceResolution referenceResolution,
        CancellationToken cancellationToken)
    {
        var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();
        ImportResolver? importResolver = null;
        foreach (var (fileIndex, file) in files.Index())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var importResolution = ImportResolution.Empty;
            if (!file.File.Imports.IsEmpty)
            {
                importResolver ??= new ImportResolver(
                    files,
                    schemaIndex.FileIndexes,
                    cancellationToken);
                importResolution = importResolver.Resolve(fileIndex);
                if (!importResolution.IsValid)
                    continue;
            }

            var missingReferences = file.References.Keys
                .Where(reference =>
                {
                    if (!schemaIndex.Owners.TryGetValue(reference, out var owner))
                        return true;

                    if (referenceResolution is not ReferenceResolution.Strict)
                        return false;

                    return owner.FileIndex == fileIndex ||
                           !importResolution.ImportedFileIndexes.Contains(owner.FileIndex);
                })
                .OrderBy(static reference => reference.FullName, StringComparer.Ordinal)
                .ToImmutableArray();
            if (!missingReferences.IsEmpty)
            {
                diagnostics.Add(MissingReferenceDiagnostic.Create(
                    LocationInfo.FromSourceText(file.File.SourceText),
                    missingReferences));
            }
        }

        if (importResolver is not null)
            diagnostics.AddRange(importResolver.Diagnostics);

        return diagnostics.ToImmutable();
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

    private sealed class SchemaIndex
    {
        public Dictionary<SchemaName, TopLevelSchema> Schemas { get; } = [];

        public Dictionary<SchemaName, SchemaOwner> Owners { get; } = [];

        public Dictionary<SchemaName, ImmutableArray<SchemaName>> Dependencies { get; } = [];

        public Dictionary<string, int> FileIndexes { get; } = new(StringComparer.Ordinal);

        public ImmutableArray<DiagnosticInfo>.Builder Diagnostics { get; } =
            ImmutableArray.CreateBuilder<DiagnosticInfo>();
    }

    private readonly record struct SchemaOwner(BoundAvroFile File, int FileIndex);

    private static bool EmitsSource(TopLevelSchema schema) =>
        schema.Type is not SchemaType.Fixed ||
        schema.CSharpName != AvroSchema.Bytes.CSharpName;
}
