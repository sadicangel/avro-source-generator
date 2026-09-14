using System.Collections.Frozen;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Compiler;

public sealed class LinkedAvroFile : IEquatable<LinkedAvroFile>
{
    private readonly Lazy<int> _hashCode;

    private LinkedAvroFile(AvroFile file, FrozenDictionary<SchemaName, CSharpName?> references)
    {
        File = file;
        References = references;

        _hashCode = new Lazy<int>(ComputeHashCode);
    }

    public AvroFile File { get; }

    public FrozenDictionary<SchemaName, CSharpName?> References { get; }

    public bool Equals(LinkedAvroFile? other) =>
        ReferenceEquals(this, other) ||
        other is not null &&
        File.Equals(other.File) &&
        References.Count == other.References.Count &&
        References.All(reference =>
            other.References.TryGetValue(reference.Key, out var csharpName) &&
            reference.Value == csharpName);

    public override bool Equals(object? obj) => Equals(obj as LinkedAvroFile);

    public override int GetHashCode() => _hashCode.Value;

    private int ComputeHashCode()
    {
        var hash = new HashCode();
        hash.Add(File);
        foreach (var reference in References.OrderBy(static reference => reference.Key.FullName, StringComparer.Ordinal))
        {
            hash.Add(reference.Key);
            hash.Add(reference.Value);
        }
        return hash.ToHashCode();
    }

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

        return new LinkedAvroFile(file, references.ToFrozenDictionary());
    }
}
