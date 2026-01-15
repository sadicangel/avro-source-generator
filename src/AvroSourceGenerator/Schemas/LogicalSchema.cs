using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AvroSourceGenerator.Schemas;

internal sealed record class LogicalSchema(
    AvroSchema UnderlyingSchema,
    CSharpName CSharpName,
    SchemaName SchemaName,
    ImmutableSortedDictionary<string, JsonElement> Properties)
    : AvroSchema(SchemaType.Logical, CSharpName, SchemaName)
{
    public override void WriteTo(Utf8JsonWriter writer, HashSet<SchemaName> writtenSchemas, string? containingNamespace)
    {
        if (UnderlyingSchema is PrimitiveSchema)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("type");
            UnderlyingSchema.WriteTo(writer, writtenSchemas, containingNamespace);
            writer.WriteString("logicalType", SchemaName.Name);
            foreach (var entry in Properties)
            {
                writer.WritePropertyName(entry.Key);
                entry.Value.WriteTo(writer);
            }

            writer.WriteEndObject();
            return;
        }

        var jsonNode = ParseSchema(UnderlyingSchema, writtenSchemas, containingNamespace);
        var valueKind = jsonNode.Root.GetValueKind();

        switch (valueKind)
        {
            case JsonValueKind.String:
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("type");
                    jsonNode.Root.WriteTo(writer);
                    writer.WriteString("logicalType", SchemaName.Name);
                    foreach (var entry in Properties)
                    {
                        writer.WritePropertyName(entry.Key);
                        entry.Value.WriteTo(writer);
                    }

                    writer.WriteEndObject();
                }
                return;

            case JsonValueKind.Object:
                {
                    var jsonObject = jsonNode.AsObject();
                    jsonObject["logicalType"] = SchemaName.Name;
                    foreach (var entry in Properties)
                    {
                        jsonObject[entry.Key] = JsonValue.Create(entry.Value);
                    }

                    jsonObject.WriteTo(writer);
                }
                return;

            case JsonValueKind.Undefined:
            case JsonValueKind.Array:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
            default:
                throw new InvalidOperationException();
        }

        static JsonNode ParseSchema(AvroSchema schema, HashSet<SchemaName> writtenSchemas, string? containingNamespace)
        {
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);
            schema.WriteTo(writer, writtenSchemas, containingNamespace);
            writer.Flush();
            stream.Position = 0;
            return JsonNode.Parse(stream) ?? throw new InvalidOperationException("Schema can't be null");
        }
    }
}
