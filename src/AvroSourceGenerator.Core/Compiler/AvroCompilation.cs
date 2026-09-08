using System.Collections.Immutable;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Compiler;

public sealed class AvroCompilation : IEquatable<AvroCompilation>
{
    private readonly ImmutableArray<BoundAvroFile> _files;
    private readonly AvroCompilationOptions _options;
    private readonly Dictionary<SchemaName, TopLevelSchema> _schemas;
    private readonly Dictionary<SchemaName, SchemaOwner> _owners;
    private readonly Dictionary<SchemaName, ImmutableArray<SchemaName>> _dependencies;

    private AvroCompilation(
        ImmutableArray<BoundAvroFile> files,
        AvroCompilationOptions options,
        Dictionary<SchemaName, TopLevelSchema> schemas,
        Dictionary<SchemaName, SchemaOwner> owners,
        Dictionary<SchemaName, ImmutableArray<SchemaName>> dependencies,
        ImmutableArray<AvroDiagnostic> diagnostics,
        bool isValid)
    {
        _files = files;
        _options = options;
        _schemas = schemas;
        _owners = owners;
        _dependencies = dependencies;
        Diagnostics = diagnostics;
        IsValid = isValid;
    }

    public ImmutableArray<BoundAvroFile> Files => _files;
    public IReadOnlyDictionary<SchemaName, TopLevelSchema> Schemas => _schemas;
    public AvroCompilationOptions Options => _options;
    public ImmutableArray<AvroDiagnostic> Diagnostics { get; }

    public bool IsValid { get; }

    public bool Equals(AvroCompilation? other) =>
        ReferenceEquals(this, other) || (other is not null && _options == other._options && _files.SequenceEqual(other._files));

    public override bool Equals(object? obj) => obj is AvroCompilation other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_options);
        foreach (var file in _files)
            hash.Add(file);
        return hash.ToHashCode();
    }

    public static AvroCompilation Create(ImmutableArray<BoundAvroFile> files, AvroCompilationOptions options, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var diagnostics = files.SelectMany(static file => file.File.Diagnostics).ToImmutableArray();

        var schemaIndex = BuildSchemaIndex(
            files,
            options.DuplicateResolution,
            cancellationToken);
        diagnostics = diagnostics
            .AddRange(schemaIndex.Diagnostics)
            .AddRange(
                ValidateReferences(
                    files,
                    schemaIndex,
                    options.ReferenceResolution,
                    cancellationToken));

        return new AvroCompilation(
            files,
            options,
            schemaIndex.Schemas,
            schemaIndex.Owners,
            schemaIndex.Dependencies,
            diagnostics,
            isValid: !diagnostics.HasErrors);
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
                    schemaIndex.Diagnostics.Add(AvroDiagnostic.DuplicateSchema(SourceSpan.None, declaration.CSharpName.ToString(includeGlobalPrefix: false)));
                    continue;
                }

                if (!schemaIndex.Schemas.TryAdd(name, declaration))
                {
                    if (duplicateResolution is DuplicateResolution.Error)
                        schemaIndex.Diagnostics.Add(AvroDiagnostic.DuplicateSchema(SourceSpan.None, declaration.CSharpName.ToString(includeGlobalPrefix: false)));
                    continue;
                }

                schemaIndex.Owners.Add(name, new SchemaOwner(file, fileIndex));
                schemaIndex.Dependencies.Add(
                    name,
                    file.Dependencies.GetValueOrDefault(name, []));
            }
        }

        return schemaIndex;
    }

    private static ImmutableArray<AvroDiagnostic> ValidateReferences(
        ImmutableArray<BoundAvroFile> files,
        SchemaIndex schemaIndex,
        ReferenceResolution referenceResolution,
        CancellationToken cancellationToken)
    {
        var diagnostics = ImmutableArray.CreateBuilder<AvroDiagnostic>();
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

                    return owner.FileIndex == fileIndex || !importResolution.Contains(owner.FileIndex);
                })
                .OrderBy(static reference => reference.FullName, StringComparer.Ordinal)
                .ToImmutableArray();

            if (!missingReferences.IsEmpty)
                diagnostics.Add(AvroDiagnostic.MissingReferences(SourceSpan.FromSourceText(file.File.SourceText), missingReferences));
        }

        if (importResolver is not null)
            diagnostics.AddRange(importResolver.Diagnostics);

        return diagnostics.ToImmutable();
    }

    /// <summary>Returns declarations owned by the supplied instance from <see cref="Files"/>.</summary>
    public ImmutableArray<TopLevelSchema> GetOwnedDeclarations(BoundAvroFile file, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var declarations = ImmutableArray.CreateBuilder<TopLevelSchema>();
        foreach (var declaration in file.Declarations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_owners.TryGetValue(declaration.SchemaName, out var owner) && ReferenceEquals(owner.File, file))
                declarations.Add(declaration);
        }
        return declarations.DrainToImmutable();
    }

    /// <summary>Returns the distinct transitive owners of the supplied schemas, ordered by file path.</summary>
    public ImmutableArray<BoundAvroFile> GetContributingFiles(IEnumerable<SchemaName> roots, CancellationToken cancellationToken = default) =>
    [
        .. GetDependencyClosure(roots, cancellationToken)
            .Select(name => _owners[name].File)
            .Distinct()
            .OrderBy(static file => file.File.SourceText.Path, StringComparer.Ordinal)
    ];

    private HashSet<SchemaName> GetDependencyClosure(IEnumerable<SchemaName> roots, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var visited = new HashSet<SchemaName>();
        var pending = new Stack<SchemaName>(roots);
        while (pending.TryPop(out var schema))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_schemas.ContainsKey(schema) || !visited.Add(schema))
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

        return visited;
    }

    private sealed class SchemaIndex
    {
        public Dictionary<SchemaName, TopLevelSchema> Schemas { get; } = [];

        public Dictionary<SchemaName, SchemaOwner> Owners { get; } = [];

        public Dictionary<SchemaName, ImmutableArray<SchemaName>> Dependencies { get; } = [];

        public Dictionary<string, int> FileIndexes { get; } = new Dictionary<string, int>(StringComparer.Ordinal);

        public ImmutableArray<AvroDiagnostic>.Builder Diagnostics { get; } =
            ImmutableArray.CreateBuilder<AvroDiagnostic>();
    }

    private readonly record struct SchemaOwner(BoundAvroFile File, int FileIndex);
}
