using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Json;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Registry.Extensions;
using AvroSourceGenerator.Schemas;
using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.Registry;

[StructLayout(LayoutKind.Auto)]
[SuppressMessage("ReSharper", "UsageOfDefaultStructEquality")]
internal readonly partial struct SchemaRegistry(
    AvroLibrary avroLibrary,
    LanguageVersion languageVersion,
    bool useNullableReferenceTypes) : IReadOnlyCollection<TopLevelSchema>
{
    private readonly Dictionary<SchemaName, TopLevelSchema> _schemas = [];

    private static readonly HashSet<string> s_reservedProperties =
    [
        "type", "name", "namespace", "fields", "items", "size", "symbols", "values", "aliases", "order", "doc",
        "default", "logicalType"
    ];

    public int Count => _schemas.Count;

    public IEnumerator<TopLevelSchema> GetEnumerator() => _schemas.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static SchemaRegistry Register(
        JsonElement schema,
        AvroLibrary avroLibrary,
        LanguageVersion languageVersion,
        bool useNullableReferenceTypes)
    {
        var registry = new SchemaRegistry(avroLibrary, languageVersion, useNullableReferenceTypes);

        _ = registry.Schema(schema, containingNamespace: null);

        if (registry.Count == 0)
        {
            throw new InvalidSchemaException($"At least a named schema must be present in schema: {schema.GetRawText()}");
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
        var type = schema.GetString() ??
                   throw new InvalidOperationException($"Unexpected json value '{schema}'. Expected 'string'");

        return Primitives(type) 
               ?? NamedSchema(containingNamespace, type)
               ?? throw new InvalidSchemaException($"Unknown schema '{type}'");
    }

    private AvroSchema Complex(JsonElement schema, string? containingNamespace)
    {
        if (schema.TryGetProperty("logicalType", out _))
        {
            return Logical(schema, containingNamespace, GetProperties(schema));
        }

        if (schema.TryGetProperty("protocol", out _))
        {
            return Protocol(schema, containingNamespace, GetProperties(schema));
        }

        return UnderlyingSchema(schema, containingNamespace);
    }

    private AvroSchema UnderlyingSchema(JsonElement schema, string? containingNamespace)
    {
        var underlyingType = schema.GetRequiredString("type");
        
        return Primitives(underlyingType) 
               ?? NotPrimitivesAppleSauce(schema, containingNamespace)
               ;
    }

    private AvroSchema? Primitives(string type)
    {
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
            _ => null
        };
    }

    private AvroSchema NotPrimitivesAppleSauce(JsonElement schema, string? containingNamespace)
    {
        var underlyingType = schema.GetRequiredString("type");

        var underlyingSchema = underlyingType switch
        {
            "array" => (AvroSchema)Array(schema, containingNamespace, GetProperties(schema)),
            "map" => Map(schema, containingNamespace, GetProperties(schema)),
            "enum" => Enum(schema, containingNamespace, GetProperties(schema)),
            "record" => Record(schema, containingNamespace, GetProperties(schema)),
            "error" => Error(schema, containingNamespace, GetProperties(schema)),
            "fixed" => Fixed(schema, containingNamespace, GetProperties(schema)),
            _ when TryGetNamedSchema(containingNamespace, underlyingType, out var namedSchema) => namedSchema,
            _ => throw new InvalidSchemaException($"Unknown schema type '{underlyingType}' in {schema.GetRawText()}")
        };

        return underlyingSchema;
    }


    private static ImmutableSortedDictionary<string, JsonElement> GetProperties(JsonElement schema)
    {
        var properties = ImmutableSortedDictionary.CreateBuilder<string, JsonElement>();
        foreach (var property in schema.EnumerateObject()
                     .Where(property => !s_reservedProperties.Contains(property.Name)))
        {
            properties.Add(property.Name, property.Value);
        }

        return properties.ToImmutable();
    }

    private AvroSchema? NamedSchema(string? containingNamespace, string type)
    {
        if (type is var _ && _schemas.TryGetValue(type.GetRequiredSchemaName(containingNamespace),
                out var topLevelSchema) && topLevelSchema is NamedSchema namedSchema)
        {
            return namedSchema;
        }
        return null;
    }

    private bool TryGetNamedSchema(string? containingNamespace, string underlyingType, [NotNullWhen(true)]out NamedSchema? namedSchema)
    {
        var schemaName = new SchemaName(underlyingType.GetValidName(), containingNamespace);
        namedSchema = _schemas.TryGetValue(schemaName, out var topLevelSchema)
            ? topLevelSchema as NamedSchema
            : null;
        return namedSchema is not null;
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
            "object" or "bool" or "int" or "long" => value.GetRawText(),
            "float" => $"{value.GetRawText()}f",
            "double" => value.GetRawText(),
            "byte[]" => $"[{string.Join(", ", value.GetBytesFromBase64().Select(bytes => $"0x{bytes:X2}"))}]",
            "string" => value.GetRawText(),
            _ when _schemas.TryGetValue(type.SchemaName, out var namedSchema) && namedSchema.Type is SchemaType.Enum =>
                $"{type}.{value.GetString()}",

            // TODO: Do we need to handle complex types? Should they be supported?
            _ => null,
        };
    }
}
