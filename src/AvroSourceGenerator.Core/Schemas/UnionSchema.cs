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
        var useNullableAnnotation = schemas.Any(static schema => schema.Type is SchemaType.Null)
            && (useNullableReferenceTypes || MapsToValueType(underlyingSchema.Type));
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

    // TODO: Can we extend this to Fixed and Error types in the future?
    public bool SupportsVariant()
    {
        if (Schemas is [] or [_] or [{ Type: SchemaType.Null }, _] or [_, { Type: SchemaType.Null }])
        {
            // Empty union, single type union, or union with nulls only are not eligible for generating abstract base records.
            return false;
        }

        // Check if all schemas are either Record or Null, and at least one is not Null.
        return Schemas.All(x => x.Type is SchemaType.Record or SchemaType.Null)
            && Schemas.Any(x => x.Type is not SchemaType.Null);
    }

    public UnionSchema WithVariant(VariantSchema variant)
    {
        // TODO: Can we extend this to Fixed and Error types in the future?
        foreach (var record in Schemas.OfType<RecordSchema>())
        {
            record.InheritsFrom = variant;
        }

        return this with
        {
            CSharpName = CSharpName.HasNullableAnnotation
                ? variant.CSharpName.WithNullableAnnotation()
                : variant.CSharpName.WithoutNullableAnnotation(),
            UnderlyingSchema = variant
        };
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
            [{ Type: not SchemaType.Null } t1, { Type: SchemaType.Null }] => t1,
            // "null" | T2
            [{ Type: SchemaType.Null }, { Type: not SchemaType.Null } t2] => t2,
            // T1 | T2 | ... | Tn
            _ => AvroSchema.Object,
        };

        while (underlyingSchema is UnionSchema { Schemas: var unionSchemas })
            underlyingSchema = GetUnderlyingSchema(unionSchemas);

        return underlyingSchema;
    }
}
