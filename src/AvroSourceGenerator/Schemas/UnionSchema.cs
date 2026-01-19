using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed record class UnionSchema(
    CSharpName CSharpName,
    ImmutableArray<AvroSchema> Schemas,
    AvroSchema UnderlyingSchema,
    bool IsNullable)
    : AvroSchema(SchemaType.Union, CSharpName, new SchemaName(string.Empty))
{
    public override void WriteTo(Utf8JsonWriter writer, HashSet<SchemaName> writtenSchemas, string? containingNamespace)
    {
        writer.WriteStartArray();
        foreach (var schema in Schemas)
            schema.WriteTo(writer, writtenSchemas, containingNamespace);
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
            CSharpName = new CSharpName(
                variant.CSharpName.Name + (IsNullable ? "?" : ""),
                variant.CSharpName.Namespace),
            UnderlyingSchema = variant
        };
    }
}
