using System.Collections.Immutable;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Inputs;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Output;

internal sealed class ImportResolver(
    ImmutableArray<BoundAvroFile> files,
    IReadOnlyDictionary<string, int> fileIndexes,
    CancellationToken cancellationToken)
{
    private readonly Dictionary<int, ImportResolution> _resolutions = [];
    private readonly HashSet<int> _visiting = [];
    private readonly List<int> _stack = [];
    private readonly HashSet<int> _cycleFiles = [];
    private readonly HashSet<string> _reportedCycles = new(StringComparer.Ordinal);
    private readonly ImmutableArray<DiagnosticInfo>.Builder _diagnostics =
        ImmutableArray.CreateBuilder<DiagnosticInfo>();

    public ImmutableArray<DiagnosticInfo> Diagnostics => _diagnostics.ToImmutable();

    public ImportResolution Resolve(int fileIndex)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_resolutions.TryGetValue(fileIndex, out var existing))
            return existing;

        var file = files[fileIndex];
        if (file.File.Imports.IsEmpty)
            return file.File.IsValid ? ImportResolution.Empty : ImportResolution.Invalid;

        if (_visiting.Contains(fileIndex))
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
                _diagnostics.Add(ImportDiagnostic(
                    files[_stack[^1]],
                    $"Import cycle detected: {string.Join(" -> ", displayPaths)}."));
            }

            foreach (var cycleFileIndex in cycle)
                _cycleFiles.Add(cycleFileIndex);

            return ImportResolution.Invalid;
        }

        _visiting.Add(fileIndex);
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
                _diagnostics.Add(ImportDiagnostic(
                    file,
                    $"Import kind '{GetImportKindName(import.Kind)}' requires a '{expectedExtension}' target, but '{import.Path}' was specified."));
                continue;
            }

            var importedPath = ImportPathResolver.Resolve(file.File.SourceText.Path, import.Path);
            if (!fileIndexes.TryGetValue(importedPath, out var importedFileIndex))
            {
                isValid = false;
                _diagnostics.Add(ImportDiagnostic(
                    file,
                    $"Path '{import.Path}' does not match an Avro AdditionalFile."));
                continue;
            }

            var importedFile = files[importedFileIndex];
            if (!IsCompatibleTarget(import.Kind, importedFile))
            {
                isValid = false;
                _diagnostics.Add(ImportDiagnostic(
                    file,
                    $"Path '{import.Path}' is not a valid {GetImportKindName(import.Kind)} target."));
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
            importedFileIndexes.ToImmutableHashSet());
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

    private static DiagnosticInfo ImportDiagnostic(BoundAvroFile file, string message) =>
        InvalidImportDiagnostic.Create(
            LocationInfo.FromSourceText(file.File.SourceText),
            message);
}

internal readonly record struct ImportResolution(
    bool IsValid,
    ImmutableHashSet<int> ImportedFileIndexes)
{
    public static readonly ImportResolution Empty = new(true, []);
    public static readonly ImportResolution Invalid = new(false, []);
}
