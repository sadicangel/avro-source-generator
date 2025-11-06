using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

internal readonly partial struct SchemaRegistry
{
    private UnionSchema Union(JsonElement schema, string? containingNamespace)
    {
        var builder = ImmutableArray.CreateBuilder<AvroSchema>();
        foreach (var innerSchema in schema.EnumerateArray())
            builder.Add(Schema(innerSchema, containingNamespace));
        var schemas = builder.ToImmutable();

        var isNullable = schemas.Any(static schema => schema.Type == SchemaType.Null);
        var underlyingSchema = GetUnderlyingSchema(schemas);
        while (underlyingSchema is UnionSchema { Schemas: var unionSchemas })
            underlyingSchema = GetUnderlyingSchema(unionSchemas);
        var hasQuestionMark = isNullable && (useNullableReferenceTypes || MapsToValueType(underlyingSchema.Type));
        var csharpName = new CSharpName(
            underlyingSchema.CSharpName.Name + (hasQuestionMark ? "?" : ""),
            underlyingSchema.CSharpName.Namespace);

        return new UnionSchema(csharpName, schemas, underlyingSchema, isNullable);

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

        static AvroSchema GetUnderlyingSchema(ImmutableArray<AvroSchema> schemas)
        {
            return schemas switch
            {
                // T1
                [var t1] => t1,
                // "null" | "null"
                [{ Type: SchemaType.Null }, { Type: SchemaType.Null }] => AvroSchema.Object,
                // T1 | "null"
                [var t1, { Type: SchemaType.Null }] => t1,
                // "null" | T2
                [{ Type: SchemaType.Null }, var t2] => t2,
                // T1 | T2 | ... | Tn
                _ => AvroSchema.Object,
            };
        }
    }
}
