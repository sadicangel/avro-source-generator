using System.Collections.Immutable;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Compiler;

internal sealed class ImportResolver(
    ImmutableArray<BoundAvroFile> files,
    IReadOnlyDictionary<SourcePath, int> fileIndexes,
    CancellationToken cancellationToken)
{
    private readonly Dictionary<int, ImportResolution> _resolutions = [];
    private readonly HashSet<int> _visiting = [];
    private readonly List<int> _stack = [];
    private readonly HashSet<int> _cycleFiles = [];
    private readonly HashSet<string> _reportedCycles = new(StringComparer.Ordinal);
    private readonly List<AvroDiagnostic> _diagnostics = [];

    public IEnumerable<AvroDiagnostic> Diagnostics => _diagnostics;

    public ImportResolution Resolve(int fileIndex) => Resolve(fileIndex, SourceSpan.None);

    private readonly struct Cycle(int[] indices)
    {
        public IEnumerable<int> Indices => indices;

        public string Key { get; } = string.Join(",", indices.OrderBy(static index => index));

        public string GetPath(ImmutableArray<BoundAvroFile> files) =>
            string.Join(" -> ", indices.Append(indices[0]).Select(index => files[index].Path.ToString()));
    }

    private ImportResolution Resolve(int fileIndex, SourceSpan incomingImportSpan)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_resolutions.TryGetValue(fileIndex, out var existing))
            return existing;

        var file = files[fileIndex];
        if (file.File.Imports.IsEmpty)
            return file.IsValid ? ImportResolution.Empty : ImportResolution.Invalid;

        if (!_visiting.Add(fileIndex))
        {
            var cycleStart = _stack.IndexOf(fileIndex);
            var cycle = new Cycle(_stack.Skip(cycleStart).ToArray());
            if (_reportedCycles.Add(cycle.Key))
            {
                _diagnostics.Add(
                    AvroDiagnostic.ImportCycle(incomingImportSpan, cycle.GetPath(files)));
            }

            foreach (var cycleFileIndex in cycle.Indices)
                _cycleFiles.Add(cycleFileIndex);

            return ImportResolution.Invalid;
        }

        _stack.Add(fileIndex);
        var isValid = file.IsValid;
        var importedFileIndexes = new HashSet<int>();
        foreach (var import in file.File.Imports)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var importSpan = import.SourceSpan;
            var expectedExtension = GetExpectedExtension(import.Kind);
            if (!import.Path.EndsWith(expectedExtension, StringComparison.OrdinalIgnoreCase))
            {
                isValid = false;
                _diagnostics.Add(AvroDiagnostic.InvalidImportFileExtension(importSpan, GetImportKindName(import.Kind), expectedExtension, import.Path));
                continue;
            }

            var importedPath = file.Path.Resolve(import.Path);
            if (!fileIndexes.TryGetValue(importedPath, out var importedFileIndex))
            {
                isValid = false;
                _diagnostics.Add(AvroDiagnostic.MissingImport(importSpan, import.Path));
                continue;
            }

            var importedFile = files[importedFileIndex];
            // A failed parse is the actionable error; its missing target shape is secondary.
            if (!importedFile.IsValid)
            {
                isValid = false;
                continue;
            }

            if (!IsCompatibleTarget(import.Kind, importedFile))
            {
                isValid = false;
                _diagnostics.Add(AvroDiagnostic.InvalidImportTarget(importSpan, import.Path, GetImportKindName(import.Kind)));
                continue;
            }

            if (!importedFileIndexes.Add(importedFileIndex))
                continue;

            var importedResolution = Resolve(importedFileIndex, importSpan);
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
        if (!file.IsValid)
            return true;

        if (!file.Path.TryGetSourceType(out var sourceType))
            return false;

        return kind switch
        {
            AvroImportKind.Idl => sourceType is SourceType.Avdl,
            AvroImportKind.Protocol =>
                sourceType is SourceType.Avpr &&
                file.RootSchema is ProtocolSchema,
            AvroImportKind.Schema =>
                sourceType is SourceType.Avsc &&
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

    public static readonly ImportResolution Empty = new(true, []);
    public static readonly ImportResolution Invalid = new(false, []);
}
