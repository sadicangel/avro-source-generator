using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Protocols;

public sealed record class ProtocolMessage(
    MemberName MethodName,
    string? Documentation,
    ImmutableArray<ProtocolRequestParameter> RequestParameters,
    ProtocolResponse Response,
    AvroSchema? Errors,
    bool? OneWay)
{
    public void WriteTo(Utf8JsonWriter writer, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, HashSet<SchemaName> writtenSchemas, string? containingNamespace)
    {
        writer.WriteStartObject();
        if (Documentation is not null)
            writer.WriteString(AvroJsonKeys.Doc, Documentation);

        writer.WritePropertyName(AvroJsonKeys.Request);
        writer.WriteStartArray();
        foreach (var parameter in RequestParameters)
            parameter.WriteTo(writer, registeredSchemas, writtenSchemas, containingNamespace);

        writer.WriteEndArray();
        writer.WritePropertyName(AvroJsonKeys.Response);
        Response.WriteTo(writer, registeredSchemas, writtenSchemas, containingNamespace);
        if (OneWay is { } oneWay)
            writer.WriteBoolean(AvroJsonKeys.OneWay, oneWay);

        switch (Errors)
        {
            case UnionSchema union:
                if (!union.Schemas.Any(static error => error.SchemaType is not SchemaType.String)) break;
                writer.WriteStartArray(AvroJsonKeys.Errors);
                foreach (var error in union.Schemas)
                {
                    if (error.SchemaType is SchemaType.String) continue;
                    error.WriteTo(writer, registeredSchemas, writtenSchemas, containingNamespace);
                }
                writer.WriteEndArray();
                break;

            case AvroSchema { SchemaType: not SchemaType.String } schema:
                writer.WriteStartArray(AvroJsonKeys.Errors);
                schema.WriteTo(writer, registeredSchemas, writtenSchemas, containingNamespace);
                writer.WriteEndArray();
                break;
        }

        writer.WriteEndObject();
    }
}
