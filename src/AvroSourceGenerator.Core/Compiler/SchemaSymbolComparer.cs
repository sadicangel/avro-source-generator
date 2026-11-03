using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Compiler;

// Binding a named reference consumes its type and names, not its declaration body.
internal sealed class SchemaSymbolComparer : IEqualityComparer<TopLevelSchema?>
{
    public static SchemaSymbolComparer Instance { get; } = new SchemaSymbolComparer();

    public bool Equals(TopLevelSchema? x, TopLevelSchema? y) =>
        ReferenceEquals(x, y) || (x is not null && y is not null
            && x.SchemaType == y.SchemaType && x.SchemaName == y.SchemaName && x.CSharpName == y.CSharpName);

    public int GetHashCode(TopLevelSchema? schema) => schema is null ? 0 : HashCode.Combine(schema.SchemaType, schema.SchemaName, schema.CSharpName);
}
