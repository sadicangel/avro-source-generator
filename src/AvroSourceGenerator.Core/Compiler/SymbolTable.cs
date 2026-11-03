using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Compiler;

public sealed class SymbolTable : IEquatable<SymbolTable>, IReadOnlyDictionary<SchemaName, TopLevelSchema>
{
    private readonly FrozenDictionary<SchemaName, TopLevelSchema> _symbols;
    private readonly Lazy<int> _hashCode;

    private SymbolTable(FrozenDictionary<SchemaName, TopLevelSchema> symbols)
    {
        _symbols = symbols;
        _hashCode = new Lazy<int>(ComputeHashCode);
    }

    public IEnumerable<SchemaName> Keys => _symbols.Keys;
    public IEnumerable<TopLevelSchema> Values => _symbols.Values;
    public int Count => _symbols.Count;

    public TopLevelSchema this[SchemaName key] => _symbols[key];

    public bool Equals(SymbolTable? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && _symbols.Count == other._symbols.Count
            && _symbols.All(kvp => other._symbols.TryGetValue(kvp.Key, out var value) && SchemaSymbolComparer.Instance.Equals(kvp.Value, value)));

    public override bool Equals(object? obj) => obj is SymbolTable other && Equals(other);

    public override int GetHashCode() => _hashCode.Value;

    private int ComputeHashCode()
    {
        var hash = new HashCode();
        foreach (var (key, value) in _symbols.OrderBy(static x => x.Key.FullName, StringComparer.Ordinal))
        {
            hash.Add(key);
            hash.Add(value, SchemaSymbolComparer.Instance);
        }
        return hash.ToHashCode();
    }

    public bool ContainsKey(SchemaName key) => _symbols.ContainsKey(key);
    public bool TryGetValue(SchemaName key, out TopLevelSchema value) => _symbols.TryGetValue(key, out value!);
    public IEnumerator<KeyValuePair<SchemaName, TopLevelSchema>> GetEnumerator() => _symbols.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static SymbolTable FromFiles(
        ImmutableArray<AvroFile> files,
        CancellationToken cancellationToken)
    {
        var symbols = new Dictionary<SchemaName, TopLevelSchema>();
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!file.IsValid)
                continue;

            foreach (var declaration in file.Declarations)
            {
                symbols.TryAdd(declaration.SchemaName, declaration);
            }
        }

        return new SymbolTable(symbols.ToFrozenDictionary());
    }
}
