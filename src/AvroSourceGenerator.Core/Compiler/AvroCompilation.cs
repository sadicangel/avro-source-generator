using System.Collections.Frozen;
using System.Collections.Immutable;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Compiler;

public sealed class AvroCompilation : IEquatable<AvroCompilation>
{
    private readonly Lazy<int> _hashCode;
    private readonly FrozenDictionary<SchemaName, SchemaOwner> _owners;
    private readonly FrozenDictionary<SchemaName, ImmutableArray<SchemaName>> _dependencies;

    private AvroCompilation(
        ImmutableArray<BoundAvroFile> files,
        AvroCompilationOptions options,
        FrozenDictionary<SchemaName, TopLevelSchema> schemas,
        FrozenDictionary<SchemaName, SchemaOwner> owners,
        FrozenDictionary<SchemaName, ImmutableArray<SchemaName>> dependencies,
        ImmutableArray<AvroDiagnostic> diagnostics,
        bool isValid)
    {
        Files = files;
        _hashCode = new Lazy<int>(ComputeHashCode);
        Options = options;
        Schemas = schemas;
        _owners = owners;
        _dependencies = dependencies;
        Diagnostics = diagnostics;
        IsValid = isValid;
    }

    public ImmutableArray<BoundAvroFile> Files { get; }

    public FrozenDictionary<SchemaName, TopLevelSchema> Schemas { get; }

    public AvroCompilationOptions Options { get; }

    public ImmutableArray<AvroDiagnostic> Diagnostics { get; }

    public bool IsValid { get; }

    public bool Equals(AvroCompilation? other) =>
        ReferenceEquals(this, other) || (other is not null && Options == other.Options && Files.SequenceEqual(other.Files));

    public override bool Equals(object? obj) => obj is AvroCompilation other && Equals(other);

    public override int GetHashCode() => _hashCode.Value;

    private int ComputeHashCode()
    {
        var hash = new HashCode();
        hash.Add(Options);
        foreach (var file in Files)
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
            schemaIndex.Schemas.ToFrozenDictionary(),
            schemaIndex.Owners.ToFrozenDictionary(),
            schemaIndex.Dependencies.ToFrozenDictionary(),
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
            foreach (var (declarationIndex, declaration) in file.Declarations.Index())
            {
                var declarationSpan = GetDeclarationSpan(file.File, declarationIndex);
                var name = declaration.SchemaName;
                if (!localNames.Add(name))
                {
                    schemaIndex.Diagnostics.Add(AvroDiagnostic.DuplicateSchema(declarationSpan, declaration.CSharpName.ToString(includeGlobalPrefix: false)));
                    continue;
                }

                if (!schemaIndex.Schemas.TryAdd(name, declaration))
                {
                    if (duplicateResolution is DuplicateResolution.Error)
                        schemaIndex.Diagnostics.Add(AvroDiagnostic.DuplicateSchema(declarationSpan, declaration.CSharpName.ToString(includeGlobalPrefix: false)));
                    continue;
                }

                schemaIndex.Owners.Add(name, new SchemaOwner(file, fileIndex));
                schemaIndex.Dependencies.Add(name, file.Dependencies.GetValueOrDefault(name, []));
            }
        }

        return schemaIndex;
    }

    private static SourceSpan GetDeclarationSpan(AvroFile file, int declarationIndex) =>
        declarationIndex < file.DeclarationSpans.Length
            ? file.DeclarationSpans[declarationIndex]
            : SourceSpan.None;

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
            // Invalid sources already carry primary diagnostics and have no linkable declarations.
            if (!file.File.IsValid)
                continue;

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
                    // The reference was not declared in any file.
                    if (!schemaIndex.Owners.TryGetValue(reference, out var owner))
                        return true;

                    // Since the reference was declared, and we're not using strict resolution, the reference is valid as long as it's declared anywhere.
                    if (referenceResolution is not ReferenceResolution.Strict)
                        return false;

                    // The reference is a forward reference, or the reference was declared in a file that is not explicitly imported by the current file.
                    return owner.FileIndex == fileIndex || !importResolution.Contains(owner.FileIndex);
                })
                .OrderBy(static reference => reference.FullName, StringComparer.Ordinal)
                .ToImmutableArray();

            if (!missingReferences.IsEmpty)
                diagnostics.Add(
                    AvroDiagnostic.MissingReferences(
                        missingReferences.SelectMany(name => file.File.ReferenceSpans.GetValueOrDefault(name, []))
                            .OrderBy(span => span.Offset)
                            .DefaultIfEmpty(SourceSpan.FromSourceText(file.File.SourceText))
                            .First(),
                        missingReferences));
        }

        if (importResolver is not null)
            diagnostics.AddRange(importResolver.Diagnostics);

        return diagnostics.ToImmutable();
    }

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
            if (!Schemas.ContainsKey(schema) || !visited.Add(schema))
                continue;

            if (!_dependencies.TryGetValue(schema, out var dependencies))
                continue;
            for (var index = dependencies.Length - 1; index >= 0; index--)
            {
                var dependency = dependencies[index];
                if (Schemas.ContainsKey(dependency))
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

        public Dictionary<string, int> FileIndexes { get; } = new(StringComparer.Ordinal);

        public ImmutableArray<AvroDiagnostic>.Builder Diagnostics { get; } =
            ImmutableArray.CreateBuilder<AvroDiagnostic>();
    }

    private readonly record struct SchemaOwner(BoundAvroFile File, int FileIndex);
}
