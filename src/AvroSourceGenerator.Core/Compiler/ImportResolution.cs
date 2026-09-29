namespace AvroSourceGenerator.Compiler;

// Ownership of the set is transferred by the resolver; callers only read the completed set.
internal readonly struct ImportResolution(bool isValid, HashSet<int> importedFileIndices)
{
    public bool IsValid { get; } = isValid;
    public bool Contains(int fileIndex) => importedFileIndices.Contains(fileIndex);
    public IEnumerable<int> ImportedFileIndices => importedFileIndices;

    public static readonly ImportResolution Empty = new(true, []);
    public static readonly ImportResolution Invalid = new(false, []);
}
