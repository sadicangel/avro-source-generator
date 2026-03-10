using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

public readonly partial struct SchemaRegistry
{
    private ImmutableArray<NamedSchema> ProtocolTypes(JsonElement.ArrayEnumerator schemas, string? containingNamespace)
    {
        var types = ImmutableArray.CreateBuilder<NamedSchema>();
        foreach (var type in schemas)
            types.Add(NamedSchema(type, containingNamespace));

        return types.ToImmutable();
    }

    private NamedSchema NamedSchema(JsonElement schema, string? containingNamespace)
    {
        var type = schema.GetSchemaType();

        return type switch
        {
            AvroTypeNames.Enum => Enum(schema, containingNamespace),
            AvroTypeNames.Record => Record(schema, containingNamespace),
            AvroTypeNames.Error => Error(schema, containingNamespace),
            AvroTypeNames.Fixed => Fixed(schema, containingNamespace),
            _ => throw new InvalidSchemaException($"Unknown schema type '{type}' in {schema.GetRawText()}")
        };
    }
}
