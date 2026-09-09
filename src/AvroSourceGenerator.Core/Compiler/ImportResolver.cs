using System.Collections.Immutable;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Compiler;

internal sealed class ImportResolver(
    ImmutableArray<BoundAvroFile> files,
    IReadOnlyDictionary<string, int> fileIndexes,
    CancellationToken cancellationToken)
{
    private readonly Dictionary<int, ImportResolution> _resolutions = [];
    private readonly HashSet<int> _visiting = [];
    private readonly List<int> _stack = [];
    private readonly HashSet<int> _cycleFiles = [];
    private readonly HashSet<string> _reportedCycles = new HashSet<string>(StringComparer.Ordinal);

    private readonly ImmutableArray<AvroDiagnostic>.Builder _diagnostics =
        ImmutableArray.CreateBuilder<AvroDiagnostic>();

    public ImmutableArray<AvroDiagnostic> Diagnostics => _diagnostics.ToImmutable();

    public ImportResolution Resolve(int fileIndex)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_resolutions.TryGetValue(fileIndex, out var existing))
            return existing;

        var file = files[fileIndex];
        if (file.File.Imports.IsEmpty)
            return file.File.IsValid ? ImportResolution.Empty : ImportResolution.Invalid;

        if (!_visiting.Add(fileIndex))
        {
            var cycleStart = _stack.IndexOf(fileIndex);
            var cycle = _stack.Skip(cycleStart).ToArray();
            var cycleKey = string.Join(
                "\0",
                cycle.Select(index => files[index].File.SourceText.Path)
                    .OrderBy(static path => path, StringComparer.Ordinal));
            if (_reportedCycles.Add(cycleKey))
            {
                var displayPaths = cycle
                    .Append(fileIndex)
                    .Select(index => files[index].File.SourceText.Path);
                _diagnostics.Add(AvroDiagnostic.InvalidImport(SourceSpan.FromSourceText(files[_stack[^1]].File.SourceText), $"Import cycle detected: {string.Join(" -> ", displayPaths)}."));
            }

            foreach (var cycleFileIndex in cycle)
                _cycleFiles.Add(cycleFileIndex);

            return ImportResolution.Invalid;
        }

        _stack.Add(fileIndex);
        var isValid = file.File.IsValid;
        var importedFileIndexes = new HashSet<int>();
        foreach (var import in file.File.Imports)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var expectedExtension = GetExpectedExtension(import.Kind);
            if (!import.Path.EndsWith(expectedExtension, StringComparison.OrdinalIgnoreCase))
            {
                isValid = false;
                _diagnostics.Add(AvroDiagnostic.InvalidImport(SourceSpan.FromSourceText(file.File.SourceText), $"Import kind '{GetImportKindName(import.Kind)}' requires a '{expectedExtension}' target, but '{import.Path}' was specified."));
                continue;
            }

            var importedPath = ImportPathResolver.Resolve(file.File.SourceText.Path, import.Path);
            if (!fileIndexes.TryGetValue(importedPath, out var importedFileIndex))
            {
                isValid = false;
                _diagnostics.Add(AvroDiagnostic.InvalidImport(SourceSpan.FromSourceText(file.File.SourceText), $"Path '{import.Path}' does not match an Avro AdditionalFile."));
                continue;
            }

            var importedFile = files[importedFileIndex];
            if (!IsCompatibleTarget(import.Kind, importedFile))
            {
                isValid = false;
                _diagnostics.Add(AvroDiagnostic.InvalidImport(SourceSpan.FromSourceText(file.File.SourceText), $"Path '{import.Path}' is not a valid {GetImportKindName(import.Kind)} target."));
                continue;
            }

            if (!importedFileIndexes.Add(importedFileIndex))
                continue;

            var importedResolution = Resolve(importedFileIndex);
            importedFileIndexes.UnionWith(importedResolution.ImportedFileIndexes);
            if (!importedResolution.IsValid)
                isValid = false;
        }

        importedFileIndexes.Remove(fileIndex);
        _stack.RemoveAt(_stack.Count - 1);
        _visiting.Remove(fileIndex);
        var resolution = new ImportResolution(
            isValid && !_cycleFiles.Contains(fileIndex),
            importedFileIndexes);
        _resolutions.Add(fileIndex, resolution);
        return resolution;
    }

    private static bool IsCompatibleTarget(AvroImportKind kind, BoundAvroFile file)
    {
        if (!file.File.IsValid)
            return true;

        return kind switch
        {
            AvroImportKind.Idl => file.File.SourceText.Type is SourceType.Avdl,
            AvroImportKind.Protocol =>
                file.File.SourceText.Type is SourceType.Avpr &&
                file.RootSchema is ProtocolSchema,
            AvroImportKind.Schema =>
                file.File.SourceText.Type is SourceType.Avsc &&
                file.RootSchema is not ProtocolSchema,
            _ => false,
        };
    }

    private static string GetExpectedExtension(AvroImportKind kind) => kind switch
    {
        AvroImportKind.Idl => ".avdl",
        AvroImportKind.Protocol => ".avpr",
        AvroImportKind.Schema => ".avsc",
        _ => throw new InvalidOperationException("Unreachable: Unsupported Avro import kind."),
    };

    private static string GetImportKindName(AvroImportKind kind) => kind switch
    {
        AvroImportKind.Idl => "idl",
        AvroImportKind.Protocol => "protocol",
        AvroImportKind.Schema => "schema",
        _ => throw new InvalidOperationException("Unreachable: Unsupported Avro import kind."),
    };
}

// Ownership of the set is transferred by the resolver; callers only read the completed set.
internal readonly struct ImportResolution(bool isValid, HashSet<int> importedFileIndexes)
{
    public bool IsValid { get; } = isValid;
    public bool Contains(int fileIndex) => importedFileIndexes.Contains(fileIndex);
    public IEnumerable<int> ImportedFileIndexes => importedFileIndexes;

    public static readonly ImportResolution Empty = new ImportResolution(true, []);
    public static readonly ImportResolution Invalid = new ImportResolution(false, []);
}
