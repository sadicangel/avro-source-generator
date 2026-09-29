using System.Collections.Frozen;
using System.Collections.Immutable;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Compiler;

public sealed class AvroCompilation : IEquatable<AvroCompilation>
{
    private readonly Lazy<int> _hashCode;
    private readonly FrozenDictionary<SchemaName, BoundAvroFile> _owners;
    private readonly FrozenDictionary<BoundAvroFile, FileMetadata> _fileMetadata;
    private readonly FrozenDictionary<BoundAvroFile, ImmutableArray<BoundAvroFile>> _fileDependencies;

    private AvroCompilation(ImmutableArray<BoundAvroFile> files, AvroCompilationOptions options, SchemaIndex schemaIndex)
    {
        Files = files;
        _hashCode = new Lazy<int>(ComputeHashCode);
        Options = options;
        Schemas = schemaIndex.Schemas.ToFrozenDictionary();
        _owners = schemaIndex.Owners.ToFrozenDictionary();
        _fileMetadata = schemaIndex.FileMetadata.ToFrozenDictionary(ReferenceEqualityComparer.Instance);
        _fileDependencies = schemaIndex.FileDependencies.ToFrozenDictionary(ReferenceEqualityComparer.Instance);
        Diagnostics = [.. schemaIndex.Diagnostics];
        IsValid = !schemaIndex.Diagnostics.HasErrors;
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
        var schemaIndex = BuildSchemaIndex(files, options.DuplicateResolution, cancellationToken);
        AnalyzeImports(schemaIndex, files, cancellationToken);
        ValidateReferences(schemaIndex, files, options.ReferenceResolution, cancellationToken);
        return new AvroCompilation(files, options, schemaIndex);
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
            schemaIndex.Diagnostics.AddRange(file.File.Diagnostics);
            cancellationToken.ThrowIfCancellationRequested();
            schemaIndex.FileMetadata.Add(file, new FileMetadata(fileIndex, file.IsValid));
            var filePath = file.Path;
            if (!schemaIndex.FileIndices.TryAdd(filePath, fileIndex))
            {
                var originalFile = files[schemaIndex.FileIndices[filePath]];
                schemaIndex.DuplicateFileIndices.Add(fileIndex);
                schemaIndex.Invalidate(file);
                schemaIndex.Diagnostics.Add(
                    AvroDiagnostic.DuplicateSourcePath(
                        SourceSpan.FromSourceFile(file),
                        file.Path.OriginalPath,
                        originalFile.Path.OriginalPath));
                continue;
            }

            localNames.Clear();
            foreach (var (declarationIndex, declaration) in file.Declarations.Index())
            {
                var declarationSpan = GetDeclarationSpan(file, declarationIndex);
                var name = declaration.SchemaName;
                if (!localNames.Add(name))
                {
                    schemaIndex.Invalidate(file);
                    schemaIndex.Diagnostics.Add(AvroDiagnostic.DuplicateSchema(declarationSpan, declaration.CSharpName.ToString(includeGlobalPrefix: false)));
                    continue;
                }

                if (!schemaIndex.Schemas.TryAdd(name, declaration))
                {
                    if (duplicateResolution is DuplicateResolution.Error)
                    {
                        schemaIndex.Invalidate(file);
                        schemaIndex.Diagnostics.Add(AvroDiagnostic.DuplicateSchema(declarationSpan, declaration.CSharpName.ToString(includeGlobalPrefix: false)));
                    }
                    continue;
                }

                schemaIndex.Owners.Add(name, file);
            }
        }

        return schemaIndex;
    }

    private static void AnalyzeImports(
        SchemaIndex schemaIndex,
        ImmutableArray<BoundAvroFile> files,
        CancellationToken cancellationToken)
    {
        var imports = new List<ImportEdge>?[files.Length];
        var importers = new List<int>?[files.Length];

        foreach (var (fileIndex, file) in files.Index())
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var import in file.File.Imports)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var targetIndex = schemaIndex.FileIndices.GetValueOrDefault(file.Path.Resolve(import.Path), -1);
                (imports[fileIndex] ??= []).Add(new ImportEdge(import, targetIndex));
                if (targetIndex >= 0)
                    (importers[targetIndex] ??= []).Add(fileIndex);
            }
        }

        var reachableByOwner = new Dictionary<int, bool[]>();
        foreach (var (fileIndex, file) in files.Index())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fileDependencies = new HashSet<BoundAvroFile>(ReferenceEqualityComparer.Instance);
            foreach (var dependencies in file.Dependencies.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var dependency in dependencies)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (schemaIndex.Owners.TryGetValue(dependency, out var owner))
                        fileDependencies.Add(owner);
                }
            }

            foreach (var reference in file.References.Keys)
            {
                cancellationToken.ThrowIfCancellationRequested();
                bool[]? reachesOwner = null;
                if (schemaIndex.Owners.TryGetValue(reference, out var owner))
                {
                    fileDependencies.Add(owner);
                    var ownerIndex = schemaIndex.FileMetadata[owner].FileIndex;
                    if (ownerIndex == fileIndex)
                        continue;

                    if (!reachableByOwner.TryGetValue(ownerIndex, out reachesOwner))
                    {
                        reachesOwner = new bool[files.Length];
                        var pendingImporters = new Queue<int>();
                        reachesOwner[ownerIndex] = true;
                        pendingImporters.Enqueue(ownerIndex);
                        while (pendingImporters.TryDequeue(out var targetIndex))
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (importers[targetIndex] is not { Count: > 0 } targetImporters)
                                continue;

                            foreach (var importerIndex in targetImporters)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                if (reachesOwner[importerIndex])
                                    continue;
                                reachesOwner[importerIndex] = true;
                                pendingImporters.Enqueue(importerIndex);
                            }
                        }
                        reachableByOwner.Add(ownerIndex, reachesOwner);
                    }
                }

                var visited = new HashSet<int>();
                var pending = new Stack<int>();
                pending.Push(fileIndex);
                while (pending.TryPop(out var currentIndex))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!visited.Add(currentIndex))
                        continue;

                    if (imports[currentIndex] is not { Count: > 0 } currentImports)
                        continue;

                    foreach (var (avroImport, targetIndex) in currentImports)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (reachesOwner is not null && (targetIndex < 0 || !reachesOwner[targetIndex]))
                            continue;
                        schemaIndex.UsedImports.Add(avroImport);
                        if (targetIndex >= 0)
                            pending.Push(targetIndex);
                    }
                }
            }
            schemaIndex.FileDependencies.Add(file, [.. fileDependencies]);
        }

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var import in file.File.Imports)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!schemaIndex.UsedImports.Contains(import))
                    schemaIndex.Diagnostics.Add(AvroDiagnostic.UnusedImport(import.SourceSpan, import.Path));
            }
        }
    }

    private static void ValidateReferences(
        SchemaIndex schemaIndex,
        ImmutableArray<BoundAvroFile> files,
        ReferenceResolution referenceResolution,
        CancellationToken cancellationToken)
    {
        ImportResolver? importResolver = null;
        foreach (var (fileIndex, file) in files.Index())
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Invalid sources already carry primary diagnostics and have no linkable declarations.
            if (!file.IsValid || schemaIndex.DuplicateFileIndices.Contains(fileIndex))
                continue;

            var importResolution = ImportResolution.Empty;
            if (!file.File.Imports.IsEmpty)
            {
                importResolver ??= new ImportResolver(
                    files,
                    schemaIndex.FileIndices,
                    schemaIndex.UsedImports,
                    cancellationToken);
                importResolution = importResolver.Resolve(fileIndex);
                if (!importResolution.IsValid)
                {
                    schemaIndex.Invalidate(file);
                    continue;
                }
            }

            var missingReferences = file.References.Keys
                .Where(reference =>
                {
                    // The reference was not declared in any file.
                    if (!schemaIndex.Owners.TryGetValue(reference, out var owner))
                        return true;

                    // We're not using strict resolution, the reference is valid as long as it's declared anywhere.
                    if (referenceResolution is not ReferenceResolution.Strict)
                        return false;

                    // The reference is a forward reference, or the reference was declared in a file that is not explicitly imported by the current file.
                    var ownerIndex = schemaIndex.FileMetadata[owner].FileIndex;
                    return ownerIndex == fileIndex || !importResolution.Contains(ownerIndex);
                })
                .OrderBy(static reference => reference.FullName, StringComparer.Ordinal)
                .ToImmutableArray();

            if (!missingReferences.IsEmpty)
            {
                schemaIndex.Invalidate(file);
                schemaIndex.Diagnostics.Add(
                    AvroDiagnostic.MissingReferences(
                        missingReferences.SelectMany(name => file.File.ReferenceSpans.GetValueOrDefault(name, []))
                            .OrderBy(span => span.Offset)
                            .DefaultIfEmpty(SourceSpan.FromSourceFile(file))
                            .First(),
                        missingReferences));
            }
        }

        if (importResolver is not null)
            schemaIndex.Diagnostics.AddRange(importResolver.Diagnostics);
    }

    private readonly record struct ImportEdge(AvroImport Import, int TargetIndex);

    private static SourceSpan GetDeclarationSpan(BoundAvroFile file, int declarationIndex) =>
        declarationIndex < file.File.DeclarationSpans.Length
            ? file.File.DeclarationSpans[declarationIndex]
            : SourceSpan.None;

    public bool IsFileValid(BoundAvroFile file, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _fileMetadata.TryGetValue(file, out var metadata) && metadata.IsValid;
    }

    public ImmutableArray<BoundAvroFile> GetContributingFiles(BoundAvroFile file, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_fileDependencies.ContainsKey(file))
            return [];

        var visited = new HashSet<BoundAvroFile>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<BoundAvroFile>();
        pending.Push(file);
        while (pending.TryPop(out var current))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!visited.Add(current))
                continue;

            foreach (var dependency in _fileDependencies[current])
            {
                cancellationToken.ThrowIfCancellationRequested();
                pending.Push(dependency);
            }
        }

        return [.. visited.OrderBy(static contributingFile => contributingFile.Path)];
    }

    public ImmutableArray<TopLevelSchema> GetOwnedDeclarations(BoundAvroFile file, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var declarations = ImmutableArray.CreateBuilder<TopLevelSchema>();
        foreach (var declaration in file.Declarations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_owners.TryGetValue(declaration.SchemaName, out var owner) && ReferenceEquals(owner, file))
                declarations.Add(declaration);
        }
        return declarations.DrainToImmutable();
    }
}
