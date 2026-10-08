namespace AvroSourceGenerator.Schemas;

internal sealed class SchemaComparer : IComparer<AvroSchema>
{
    public static readonly SchemaComparer Instance = new SchemaComparer();

    /// <inheritdoc />
    // First null, then primitive types, then complex types. Within each group, sort by CSharpName.FullName.
    public int Compare(AvroSchema x, AvroSchema y)
    {
        if (x.Type is SchemaType.Null) return y.Type is SchemaType.Null ? 0 : -1;
        if (y.Type is SchemaType.Null) return 1;
        var result = (x, y) switch
        {
            (PrimitiveSchema, not PrimitiveSchema) => -1,
            (not PrimitiveSchema, PrimitiveSchema) => 1,
            _ => 0
        };
        if (result != 0) return result;

        return StringComparer.Ordinal.Compare(x.CSharpName.FullName, y.CSharpName.FullName);
    }
}

internal static class SchemaComparerExtensions
{
    extension(IEnumerable<AvroSchema> schemas)
    {
        public IEnumerable<AvroSchema> Canonicalize(bool includeNull) => schemas
            .Where(s => includeNull || s.Type is not SchemaType.Null)
            .Order(SchemaComparer.Instance)
            .DistinctBy(static schema => schema.CSharpName);
    }
}
