using System.Text.Json;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Schemas.Defaults;

namespace AvroSourceGenerator.Protocols;

public sealed record class ProtocolRequestParameter(
    string Name,
    AvroSchema Type,
    AvroSchema UnderlyingType,
    string? Documentation,
    DefaultValue? Default)
{
    public void WriteTo(Utf8JsonWriter writer, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, HashSet<SchemaName> writtenSchemas, string? containingNamespace)
    {
        writer.WriteStartObject();
        writer.WriteString(AvroJsonKeys.Name, Name is ['@', ..] ? Name[1..] : Name);
        writer.WritePropertyName(AvroJsonKeys.Type);
        Type.WriteTo(writer, registeredSchemas, writtenSchemas, containingNamespace);
        if (Documentation is not null)
        {
            writer.WriteString(AvroJsonKeys.Doc, Documentation);
        }

        if (Default is not null)
        {
            writer.WritePropertyName(AvroJsonKeys.Default);
            Default.WriteTo(writer);
        }

        writer.WriteEndObject();
    }
}
