using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Compiler.Parsing;

internal sealed class UnionBranchComparer : IEqualityComparer<AvroSchema>
{
    public static readonly UnionBranchComparer Instance = new UnionBranchComparer();

    private UnionBranchComparer() { }

    public bool Equals(AvroSchema? x, AvroSchema? y) => ReferenceEquals(x, y) || string.Equals(GetSchemaId(x), GetSchemaId(y), StringComparison.Ordinal);

    public int GetHashCode(AvroSchema obj) => string.GetHashCode(GetSchemaId(obj), StringComparison.Ordinal);

    private static string? GetSchemaId(AvroSchema? schema) => schema?.SchemaType switch
    {
        null => null,
        SchemaType.Null => "<null>",
        SchemaType.Boolean => "<boolean>",
        SchemaType.Int => "<int>",
        SchemaType.Long => "<long>",
        SchemaType.Float => "<float>",
        SchemaType.Double => "<double>",
        SchemaType.Bytes => "<bytes>",
        SchemaType.String => "<string>",
        SchemaType.Array => "<array>",
        SchemaType.Map => "<map>",
        SchemaType.Union => schema.SchemaName.FullName,
        SchemaType.Enum => schema.SchemaName.FullName,
        SchemaType.Fixed => schema.SchemaName.FullName,
        SchemaType.Record => schema.SchemaName.FullName,
        SchemaType.Error => schema.SchemaName.FullName,
        SchemaType.Protocol => schema.SchemaName.FullName,
        SchemaType.Logical => GetSchemaId(((LogicalSchema)schema).UnderlyingSchema),
        SchemaType.Reference => schema.SchemaName.FullName,
        SchemaType.UnionType => throw new NotSupportedException($"Unsupported schema type: {schema.SchemaType}"),
        _ => throw new NotSupportedException($"Unsupported schema type: {schema.SchemaType}")
    };
}
