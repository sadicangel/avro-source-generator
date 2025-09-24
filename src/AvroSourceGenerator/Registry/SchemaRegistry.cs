using System.Collections;
using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Registry.Extensions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

internal readonly partial struct SchemaRegistry(bool useNullableReferenceTypes) : IReadOnlyCollection<TopLevelSchema>
{
    private readonly Dictionary<SchemaName, TopLevelSchema> _schemas = [];
    private static readonly HashSet<string> s_reservedProperties = ["type", "name", "namespace", "fields", "items", "size", "symbols", "values", "aliases", "order", "doc", "default", "logicalType"];

    public int Count => _schemas.Count;

    public IEnumerator<TopLevelSchema> GetEnumerator() => _schemas.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static SchemaRegistry Register(JsonElement schema, bool useNullableReferenceTypes)
    {
        var registry = new SchemaRegistry(useNullableReferenceTypes);

        _ = registry.Schema(schema, containingNamespace: null);

        if (registry.Count == 0)
        {
            throw new InvalidSchemaException($"Atleast a named schema must be present in schema: {schema.GetRawText()}");
        }

        return registry;
    }

    private AvroSchema Schema(JsonElement schema, string? containingNamespace)
    {
        return schema.ValueKind switch
        {
            JsonValueKind.String => WellKnown(schema, containingNamespace),
            JsonValueKind.Object => Complex(schema, containingNamespace),
            JsonValueKind.Array => Union(schema, containingNamespace),
            _ => throw new InvalidSchemaException($"Invalid schema: {schema.GetRawText()}")
        };
    }

    private AvroSchema WellKnown(JsonElement schema, string? containingNamespace)
    {
        var type = schema.GetString() ?? throw new InvalidOperationException($"Unexpected json value '{schema}'. Expected 'string'");

        return type switch
        {
            "null" => AvroSchema.Object,
            "boolean" => AvroSchema.Boolean,
            "int" => AvroSchema.Int,
            "long" => AvroSchema.Long,
            "float" => AvroSchema.Float,
            "double" => AvroSchema.Double,
            "bytes" => AvroSchema.Bytes,
            "string" => AvroSchema.String,
            _ when _schemas.TryGetValue(type.GetRequiredSchemaName(containingNamespace), out var topLevelSchema)
                && topLevelSchema is NamedSchema namedSchema => namedSchema,
            _ => throw new InvalidSchemaException($"Unknown schema '{type}'")
        };
    }

    private AvroSchema Complex(JsonElement schema, string? containingNamespace)
    {
        if (schema.TryGetProperty("logicalType", out var _))
        {
            return Logical(schema, containingNamespace, GetProperties(schema));
        }

        if (schema.TryGetProperty("protocol", out _))
        {
            return Protocol(schema, containingNamespace, GetProperties(schema));
        }

        var type = schema.GetSchemaType();

        return type switch
        {
            "array" => Array(schema, containingNamespace, GetProperties(schema)),
            "map" => Map(schema, containingNamespace, GetProperties(schema)),
            "enum" => Enum(schema, containingNamespace, GetProperties(schema)),
            "record" => Record(schema, containingNamespace, GetProperties(schema)),
            "error" => Error(schema, containingNamespace, GetProperties(schema)),
            "fixed" => Fixed(schema, containingNamespace, GetProperties(schema)),
            _ => throw new InvalidSchemaException($"Unknown schema type '{type}' in {schema.GetRawText()}")
        };
    }

    private ImmutableSortedDictionary<string, JsonElement> GetProperties(JsonElement schema)
    {
        var properties = ImmutableSortedDictionary.CreateBuilder<string, JsonElement>();
        foreach (var property in schema.EnumerateObject())
        {
            if (!s_reservedProperties.Contains(property.Name))
                properties.Add(property.Name, property.Value);
        }
        return properties.ToImmutable();
    }

    private string? GetValue(AvroSchema type, JsonElement? json)
    {
        if (json is null or { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined })
        {
            return null;
        }

        var value = json.Value;

        // TODO: Actually validate the value so that we don't generate invalid code.
        return type.CSharpName.Name switch
        {
            "object" => value.GetRawText(),
            "bool" => value.GetRawText(),
            "int" => value.GetRawText(),
            "long" => value.GetRawText(),
            "float" => $"{value.GetRawText()}f",
            "double" => value.GetRawText(),
            "byte[]" => $"[{string.Join(", ", value.GetBytesFromBase64().Select(bytes => $"0x{bytes:X2}"))}]",
            "string" => value.GetRawText(),
            _ when _schemas.TryGetValue(type.SchemaName, out var namedSchema) && namedSchema.Type is SchemaType.Enum => $"{type}.{value.GetString()}",

            // TODO: Do we need to handle complex types? Should they be supported?
            _ => null,
        };
    }
}
