using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public sealed record class UnionSchema(
    CSharpName CSharpName,
    ImmutableArray<AvroSchema> Schemas,
    AvroSchema UnderlyingSchema)
    : AvroSchema(SchemaType.Union, new SchemaName(string.Empty), CSharpName, Documentation: null, Properties: ImmutableSortedDictionary<string, JsonElement>.Empty)
{
    public static UnionSchema Create(ImmutableArray<AvroSchema> schemas, bool useNullableReferenceTypes)
    {
        var underlyingSchema = GetUnderlyingSchema(schemas);
        var useNullableAnnotation = schemas.Any(static schema => schema.SchemaType is SchemaType.Null)
            && (useNullableReferenceTypes || MapsToValueType(underlyingSchema.SchemaType));
        var csharpName = useNullableAnnotation
            ? underlyingSchema.CSharpName.WithNullableAnnotation()
            : underlyingSchema.CSharpName.WithoutNullableAnnotation();

        return new UnionSchema(csharpName, schemas, underlyingSchema);
    }

    public override void WriteTo(Utf8JsonWriter writer, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, HashSet<SchemaName> writtenSchemas, string? containingNamespace)
    {
        writer.WriteStartArray();
        foreach (var schema in Schemas)
            schema.WriteTo(writer, registeredSchemas, writtenSchemas, containingNamespace);
        writer.WriteEndArray();
    }

    private static bool MapsToValueType(SchemaType type) =>
        type is SchemaType.Boolean or SchemaType.Int or SchemaType.Long or SchemaType.Float or SchemaType.Double or SchemaType.Enum;

    private static AvroSchema GetUnderlyingSchema(ImmutableArray<AvroSchema> schemas)
    {
        var underlyingSchema = schemas switch
        {
            // T1
            [var t1] => t1,
            // T1 | "null"
            [{ SchemaType: not SchemaType.Null } t1, { SchemaType: SchemaType.Null }] => t1,
            // "null" | T2
            [{ SchemaType: SchemaType.Null }, { SchemaType: not SchemaType.Null } t2] => t2,
            // T1 | T2 | ... | Tn
            _ => Null,
        };

        while (underlyingSchema is UnionSchema { Schemas: var unionSchemas })
            underlyingSchema = GetUnderlyingSchema(unionSchemas);

        return underlyingSchema;
    }
}
