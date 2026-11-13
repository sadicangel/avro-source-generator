namespace AvroSourceGenerator.Compiler.Analysis;

// Ownership of the set is transferred by the resolver; callers only read the completed set.
internal readonly struct ImportResolution(bool isValid, HashSet<int> importedFileIndices)
{
    public static readonly ImportResolution Empty = new ImportResolution(true, []);
    public static readonly ImportResolution Invalid = new ImportResolution(false, []);

    public bool IsValid { get; } = isValid;

    public IEnumerable<int> ImportedFileIndices => importedFileIndices;

    public bool Contains(int fileIndex) => importedFileIndices.Contains(fileIndex);
}
