using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Compiler;

public sealed class LinkedAvroFile : IEquatable<LinkedAvroFile>
{
    private readonly Dictionary<SchemaName, CSharpName?> _references;
    private readonly int _hashCode;

    private LinkedAvroFile(AvroFile file, Dictionary<SchemaName, CSharpName?> references)
    {
        File = file;
        _references = references;

        var hash = new HashCode();
        hash.Add(File);
        foreach (var reference in _references.OrderBy(static reference => reference.Key.FullName, StringComparer.Ordinal))
        {
            hash.Add(reference.Key);
            hash.Add(reference.Value);
        }
        _hashCode = hash.ToHashCode();
    }

    public AvroFile File { get; }

    public IReadOnlyDictionary<SchemaName, CSharpName?> References => _references;

    public bool Equals(LinkedAvroFile? other) =>
        ReferenceEquals(this, other) ||
        other is not null &&
        File.Equals(other.File) &&
        _references.Count == other._references.Count &&
        _references.All(reference =>
            other._references.TryGetValue(reference.Key, out var csharpName) &&
            reference.Value == csharpName);

    public override bool Equals(object? obj) => obj is LinkedAvroFile other && Equals(other);

    public override int GetHashCode() => _hashCode;

    public static LinkedAvroFile Link(AvroFile file, SymbolTable symbolTable, CancellationToken cancellationToken)
    {
        var references = new Dictionary<SchemaName, CSharpName?>(file.References.Length);

        foreach (var reference in file.References)
        {
            cancellationToken.ThrowIfCancellationRequested();
            references.Add(
                reference,
                symbolTable.TryGetValue(reference, out var resolved)
                    ? resolved
                    : null);
        }

        return new LinkedAvroFile(file, references);
    }
}
