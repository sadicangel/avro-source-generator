using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Registry.Extensions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

internal readonly partial struct SchemaRegistry
{
    private ImmutableArray<ProtocolRequestParameter> ProtocolRequestParameters(JsonElement schema, string? containingNamespace)
    {
        var fields = ImmutableArray.CreateBuilder<ProtocolRequestParameter>();
        foreach (var parameter in schema.GetRequiredArray("request"))
            fields.Add(ProtocolRequestParameter(parameter, containingNamespace));

        return fields.ToImmutable();
    }

    private ProtocolRequestParameter ProtocolRequestParameter(JsonElement field, string? containingNamespace)
    {
        var name = field.GetRequiredString("name").GetValidName();
        var type = Schema(field.GetRequiredProperty("type"), containingNamespace);
        var underlyingType = type;
        var isNullable = false;
        if (type is UnionSchema union)
        {
            isNullable = union.IsNullable;
            underlyingType = union.UnderlyingSchema;
        }

        var documentation = field.GetDocumentation();
        var defaultJson = field.GetNullableProperty("default");
        var @default = GetValue(type, defaultJson);
        return new ProtocolRequestParameter(name, type, underlyingType, isNullable, documentation, defaultJson, @default);
    }
}
