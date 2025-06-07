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
}
