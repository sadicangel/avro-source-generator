using System.Collections;
using System.Collections.Immutable;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Inputs;

internal sealed class SymbolTable(Dictionary<SchemaName, CSharpName> symbols) : IEquatable<SymbolTable>, IReadOnlyDictionary<SchemaName, CSharpName>
{
    private readonly Dictionary<SchemaName, CSharpName> _symbols = symbols;

    public IEnumerable<SchemaName> Keys => _symbols.Keys;
    public IEnumerable<CSharpName> Values => _symbols.Values;
    public int Count => _symbols.Count;

    public CSharpName this[SchemaName key] => _symbols[key];

    public bool Equals(SymbolTable? other) =>
        ReferenceEquals(this, other)
        || other is not null
        && _symbols.Count == other._symbols.Count
        && _symbols.All(kvp => other._symbols.TryGetValue(kvp.Key, out var value) && kvp.Value.Equals(value));

    public override bool Equals(object? obj) => obj is SymbolTable other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var kvp in _symbols.OrderBy(
            static reference => reference.Key.FullName,
            StringComparer.Ordinal))
        {
            hash.Add(kvp.Key);
            hash.Add(kvp.Value);
        }
        return hash.ToHashCode();
    }

    public bool ContainsKey(SchemaName key) => _symbols.ContainsKey(key);
    public bool TryGetValue(SchemaName key, out CSharpName value) => _symbols.TryGetValue(key, out value);
    public IEnumerator<KeyValuePair<SchemaName, CSharpName>> GetEnumerator() => _symbols.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static SymbolTable FromFiles(
        ImmutableArray<AvroFile> files,
        CancellationToken cancellationToken)
    {
        var symbols = new Dictionary<SchemaName, CSharpName>();
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!file.IsValid)
                continue;

            foreach (var declaration in file.Declarations)
            {
                if (!symbols.ContainsKey(declaration.SchemaName))
                {
                    symbols[declaration.SchemaName] = declaration.CSharpName;
                }
            }
        }

        return new SymbolTable(symbols);
    }
}
