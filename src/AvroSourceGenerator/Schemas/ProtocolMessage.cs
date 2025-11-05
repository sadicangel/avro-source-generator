using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed record class ProtocolMessage(
    string MethodName,
    string? Documentation,
    ImmutableArray<ProtocolRequestParameter> RequestParameters,
    ProtocolResponse Response,
    ImmutableArray<AvroSchema> Errors)
{
    public void WriteTo(Utf8JsonWriter writer, HashSet<SchemaName> writtenSchemas, string? containingNamespace)
    {
        writer.WriteStartObject();
        if (Documentation is not null)
        {
            writer.WriteString("doc", Documentation);
        }

        writer.WritePropertyName("request");
        writer.WriteStartArray();
        foreach (var parameter in RequestParameters)
        {
            parameter.WriteTo(writer, writtenSchemas, containingNamespace);
        }

        writer.WriteEndArray();
        writer.WritePropertyName("response");
        Response.WriteTo(writer, writtenSchemas, containingNamespace);
        if (Errors.Length > 0)
        {
            writer.WriteStartArray("errors");
            foreach (var error in Errors)
            {
                error.WriteTo(writer, writtenSchemas, containingNamespace);
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }
}
