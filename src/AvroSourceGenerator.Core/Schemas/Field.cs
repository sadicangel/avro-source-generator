using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Schemas.Defaults;

namespace AvroSourceGenerator.Schemas;

public sealed record class Field(
    MemberName Name,
    AvroSchema Type,
    AvroSchema UnderlyingType,
    string? Documentation,
    ImmutableArray<string> Aliases,
    DefaultValue? Default,
    string? Order,
    ImmutableSortedDictionary<string, JsonElement> Properties,
    string? Remarks)
{
    public IEnumerable<AvroSchema> PossibleTypes
    {
        get
        {
            if (Type is not UnionSchema union)
            {
                yield return Type;
                yield break;
            }

            foreach (var schema in union.Schemas.Canonicalize(includeNull: false))
                yield return schema;
        }
    }

    public bool AllowsNull => Type is UnionSchema union
        ? union.Schemas.Any(static schema => schema.SchemaType is SchemaType.Null)
        : Type.SchemaType is SchemaType.Null;

    public void WriteTo(Utf8JsonWriter writer, HashSet<SchemaName> writtenSchemas, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, string? containingNamespace)
    {
        writer.WriteStartObject();
        writer.WriteString(AvroJsonKeys.Name, Name.SchemaName);
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

        if (Default is not null)
        {
            writer.WritePropertyName(AvroJsonKeys.Default);
            Default.WriteTo(writer);
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
