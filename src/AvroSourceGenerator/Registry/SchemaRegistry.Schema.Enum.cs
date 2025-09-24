using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Registry.Extensions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

internal readonly partial struct SchemaRegistry
{
    private EnumSchema Enum(JsonElement schema, string? containingNamespace, ImmutableSortedDictionary<string, JsonElement>? properties = null)
    {
        var schemaName = schema.GetRequiredSchemaName(containingNamespace);

        if (_schemas.ContainsKey(schemaName))
            throw new InvalidSchemaException($"Redeclaration of schema '{schemaName}'");

        var documentation = schema.GetDocumentation();
        var aliases = schema.GetAliases();
        var symbols = schema.GetSymbols();
        var @default = schema.GetNullableString("default");

        var enumSchema = new EnumSchema(schema, schemaName, documentation, aliases, symbols, @default, properties ?? ImmutableSortedDictionary<string, JsonElement>.Empty);
        _schemas[schemaName] = enumSchema;

        return enumSchema;
    }
}
