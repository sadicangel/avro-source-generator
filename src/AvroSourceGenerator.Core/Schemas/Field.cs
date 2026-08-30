using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public sealed record class Field(
    string Name,
    AvroSchema Type,
    AvroSchema UnderlyingType,
    string? Documentation,
    ImmutableArray<string> Aliases,
    JsonElement? DefaultJson,
    object? Default,
    string? Order,
    ImmutableSortedDictionary<string, JsonElement> Properties,
    string? Remarks)
{
    public ImmutableArray<AvroSchema> PossibleTypes => Type is UnionSchema union ? union.Schemas : [Type];

    public bool AllowsNull => PossibleTypes.Any(static schema => schema.Type is SchemaType.Null);

    public void WriteTo(Utf8JsonWriter writer, HashSet<SchemaName> writtenSchemas, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, string? containingNamespace)
    {
        writer.WriteStartObject();
        // TODO: Is it worth to store the schema name?
        writer.WriteString(AvroJsonKeys.Name, Name is ['@', ..] ? Name[1..] : Name);
        writer.WritePropertyName(AvroJsonKeys.Type);
        Type.WriteTo(writer, registeredSchemas, writtenSchemas, containingNamespace);
        if (Documentation is not null)
            writer.WriteString(AvroJsonKeys.Doc, Documentation);
        if (Aliases.Length > 0)
        {
            writer.WriteStartArray(AvroJsonKeys.Aliases);
            foreach (var alias in Aliases)
                writer.WriteStringValue(alias);
            writer.WriteEndArray();
        }

        if (DefaultJson is not null)
        {
            writer.WritePropertyName(AvroJsonKeys.Default);
            DefaultJson.Value.WriteTo(writer);
        }

        // Apache.Avro drops field order when CodeGen re-emits a schema. This is probably
        // an upstream bug, but our generated schema text intentionally stays in sync.
        // if (Order is not null)
        //     writer.WriteString(AvroJsonKeys.Order, Order);

        foreach (var entry in Properties)
        {
            writer.WritePropertyName(entry.Key);
            entry.Value.WriteTo(writer);
        }

        writer.WriteEndObject();
    }
}
