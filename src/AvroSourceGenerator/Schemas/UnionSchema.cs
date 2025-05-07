using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed record class UnionSchema(
    CSharpName CSharpName,
    ImmutableArray<AvroSchema> Schemas,
    AvroSchema UnderlyingSchema,
    bool IsNullable) : AvroSchema(SchemaType.Union, CSharpName, new SchemaName(""))
{
    public override void WriteTo(Utf8JsonWriter writer, HashSet<SchemaName> writtenSchemas, string? containingNamespace)
    {
        writer.WriteStartArray();
        foreach (var schema in Schemas)
            schema.WriteTo(writer, writtenSchemas, containingNamespace);
        writer.WriteEndArray();
    }

    public static UnionSchema FromArray(ImmutableArray<AvroSchema> schemas, bool useNullableReferenceTypes)
    {
        var isNullable = schemas.Any(static schema => schema.Type == SchemaType.Null);
        var underlyingSchema = GetUnderlyingSchema(schemas);
        while (underlyingSchema is UnionSchema union)
            underlyingSchema = GetUnderlyingSchema(union.Schemas);
        var hasQuestionMark = isNullable && (useNullableReferenceTypes || MapsToValueType(underlyingSchema.Type));
        var csharpName = new CSharpName(
            underlyingSchema.CSharpName.Name + (hasQuestionMark ? "?" : ""),
            underlyingSchema.CSharpName.Namespace);

        return new UnionSchema(csharpName, schemas, underlyingSchema, isNullable);

        static AvroSchema GetUnderlyingSchema(ImmutableArray<AvroSchema> schemas)
        {
            return schemas.Length switch
            {
                // T1
                1 => schemas[0],
                // T1 | T2
                2 => (schemas[0].Type, schemas[1].Type) switch
                {
                    // "null" | "null"
                    (SchemaType.Null, SchemaType.Null) => AvroSchema.Object,
                    // "null" | T
                    (SchemaType.Null, _) => schemas[1],
                    // T | "null"
                    (_, SchemaType.Null) => schemas[0],
                    // T1 | T2
                    _ => AvroSchema.Object,
                },
                // T1 | T2 | ... | Tn
                _ => AvroSchema.Object,
            };
        }

        static bool MapsToValueType(SchemaType type) => type switch
        {
            SchemaType.Boolean => true,
            SchemaType.Int => true,
            SchemaType.Long => true,
            SchemaType.Float => true,
            SchemaType.Double => true,
            SchemaType.Enum => true,
            _ => false,
        };
    }
}
