using System.Text.Json;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

public readonly partial struct SchemaRegistry
{
    private ProtocolResponse ProtocolResponse(JsonElement schema, string? containingNamespace)
    {
        var type = Schema(schema, containingNamespace);
        var underlyingType = type;
        var isNullable = false;
        if (type is UnionSchema union)
        {
            isNullable = union.IsNullable;
            underlyingType = union.UnderlyingSchema;
        }

        return new ProtocolResponse(type, underlyingType, isNullable);
    }
}
