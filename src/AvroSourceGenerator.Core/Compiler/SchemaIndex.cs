using System.Collections.Immutable;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Compiler;

internal readonly record struct FileMetadata(int FileIndex, bool IsValid);

internal sealed class SchemaIndex
{
    public Dictionary<SchemaName, TopLevelSchema> Schemas { get; } = [];

    public Dictionary<SchemaName, BoundAvroFile> Owners { get; } = [];

    public Dictionary<SourcePath, int> FileIndices { get; } = [];

    public HashSet<int> DuplicateFileIndices { get; } = [];

    public Dictionary<BoundAvroFile, FileMetadata> FileMetadata { get; } = new(ReferenceEqualityComparer.Instance);

    public Dictionary<BoundAvroFile, ImmutableArray<BoundAvroFile>> FileDependencies { get; } = new(ReferenceEqualityComparer.Instance);

    public HashSet<AvroImport> UsedImports { get; } = new(ReferenceEqualityComparer.Instance);

    public List<AvroDiagnostic> Diagnostics { get; } = [];

    public void Invalidate(BoundAvroFile file) => FileMetadata[file] = FileMetadata[file] with { IsValid = false };
}
