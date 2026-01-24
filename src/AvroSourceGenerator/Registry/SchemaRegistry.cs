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
    private readonly List<SchemaName> _recursionStack = [];

    private static readonly HashSet<string> s_reservedProperties =
    [
        "type", "name", "namespace", "fields", "items", "size", "symbols", "values", "aliases", "order", "doc", "default", "logicalType"
    ];

    public int Count => _schemas.Count;

    public IEnumerator<TopLevelSchema> GetEnumerator() => _schemas.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IReadOnlyDictionary<SchemaName, TopLevelSchema> Schemas => _schemas;

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

    private AvroSchema? FindByName(string name, string? containingNamespace)
    {
        switch (name)
        {
            case "null": return AvroSchema.Object;
            case "boolean": return AvroSchema.Boolean;
            case "int": return AvroSchema.Int;
            case "long": return AvroSchema.Long;
            case "float": return AvroSchema.Float;
            case "double": return AvroSchema.Double;
            case "bytes": return AvroSchema.Bytes;
            case "string": return AvroSchema.String;
        }

        var schemaName = name.ToSchemaName(containingNamespace);
        if (_schemas.TryGetValue(schemaName, out var topLevelSchema) && topLevelSchema is NamedSchema)
            return new AvroSchemaReference(topLevelSchema.SchemaName);

        var index = _recursionStack.FindLastIndex(existing => schemaName == existing);
        if (index >= 0)
            return new AvroSchemaReference(_recursionStack[index]);

        return null;
    }

    private RecursionScope Track(SchemaName schemaName) => new(_recursionStack, schemaName);

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

        return FindByName(type, containingNamespace) ?? throw new InvalidSchemaException($"Unknown schema '{type}'");
    }

    private AvroSchema Complex(JsonElement schema, string? containingNamespace)
    {
        if (schema.TryGetProperty("protocol", out _))
        {
            return Protocol(schema, containingNamespace, GetProperties(schema));
        }

        var underlyingSchema = UnderlyingSchema(schema, containingNamespace, GetProperties(schema));

        return schema.TryGetProperty("logicalType", out _) ? Logical(schema, underlyingSchema) : underlyingSchema;
    }

    private AvroSchema UnderlyingSchema(JsonElement schema, string? containingNamespace, ImmutableSortedDictionary<string, JsonElement> properties)
    {
        var type = schema.GetSchemaType();
        switch (type)
        {
            case "array": return Array(schema, containingNamespace, properties);
            case "map": return Map(schema, containingNamespace, properties);
            case "enum": return Enum(schema, containingNamespace, properties);
            case "record": return Record(schema, containingNamespace, properties);
            case "error": return Error(schema, containingNamespace, properties);
            case "fixed": return Fixed(schema, containingNamespace, properties);
        }

        var wellKnown = FindByName(type, containingNamespace)
            ?? throw new InvalidSchemaException($"Unknown schema type '{type}' in {schema.GetRawText()}");

        if (wellKnown is PrimitiveSchema primitive)
        {
            return primitive with { Properties = properties };
        }

        // TODO: Should we add/merge properties for other schema types?
        return wellKnown;
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

    private readonly ref struct RecursionScope : IDisposable
    {
        private readonly List<SchemaName> _recursionStack;
        private readonly SchemaName _schemaName;

        public RecursionScope(List<SchemaName> recursionStack, SchemaName schemaName)
        {
            _recursionStack = recursionStack;
            _schemaName = schemaName;

            if (_recursionStack.Contains(schemaName))
            {
                throw new InvalidSchemaException($"Recursive schema definition detected for schema '{schemaName}'.");
            }

            _recursionStack.Add(schemaName);
        }

        public void Dispose()
        {
            if (_recursionStack is not [.., var popped] || popped != _schemaName)
            {
                throw new InvalidOperationException("Recursion stack corrupted.");
            }

            _recursionStack.RemoveAt(_recursionStack.Count - 1);
        }
    }
}
