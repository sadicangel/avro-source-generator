using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Protocols;

public sealed record class ProtocolMessage(
    string MethodName,
    string? Documentation,
    ImmutableArray<ProtocolRequestParameter> RequestParameters,
    ProtocolResponse Response,
    ImmutableArray<AvroSchema> Errors)
{
    public void WriteTo(Utf8JsonWriter writer, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, HashSet<SchemaName> writtenSchemas, string? containingNamespace)
    {
        writer.WriteStartObject();
        if (Documentation is not null)
        {
            writer.WriteString(AvroJsonKeys.Doc, Documentation);
        }

        writer.WritePropertyName(AvroJsonKeys.Request);
        writer.WriteStartArray();
        foreach (var parameter in RequestParameters)
        {
            parameter.WriteTo(writer, registeredSchemas, writtenSchemas, containingNamespace);
        }

        writer.WriteEndArray();
        writer.WritePropertyName(AvroJsonKeys.Response);
        Response.WriteTo(writer, registeredSchemas, writtenSchemas, containingNamespace);
        if (Errors.Length > 0)
        {
            writer.WriteStartArray(AvroJsonKeys.Errors);
            foreach (var error in Errors)
            {
                error.WriteTo(writer, registeredSchemas, writtenSchemas, containingNamespace);
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }
}
