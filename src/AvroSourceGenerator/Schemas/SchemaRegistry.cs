using System.Collections;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using AvroSourceGenerator.Schemas.Extensions;

namespace AvroSourceGenerator.Schemas;

internal readonly struct SchemaRegistry(bool useNullableReferenceTypes) : IReadOnlyCollection<NamedSchema>
{
    private readonly Dictionary<SchemaName, NamedSchema> _schemas = [];
    private static readonly HashSet<string> s_reservedProperties = ["type", "name", "namespace", "fields", "items", "size", "symbols", "values", "aliases", "order", "doc", "default", "logicalType"];


    public int Count => _schemas.Count;

    public IEnumerator<NamedSchema> GetEnumerator() => _schemas.Values.GetEnumerator();
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
            _ when _schemas.TryGetValue(type.GetRequiredSchemaName(containingNamespace), out var registeredSchema) => registeredSchema,
            _ => throw new InvalidSchemaException($"Unknown schema '{type}'")
        };
    }

    private AvroSchema Complex(JsonElement schema, string? containingNamespace)
    {
        var properties = GetProperties(schema);

        if (schema.TryGetProperty("logicalType", out var logicalType) && logicalType.ValueKind is JsonValueKind.String)
        {
            return Logical(schema, properties, containingNamespace);
        }

        var type = schema.GetSchemaType();

        return type switch
        {
            "array" => Array(schema, properties, containingNamespace),
            "map" => Map(schema, properties, containingNamespace),
            "enum" => Enum(schema, properties, containingNamespace),
            "record" => Record(schema, properties, containingNamespace),
            "error" => Error(schema, properties, containingNamespace),
            "fixed" => Fixed(schema, properties, containingNamespace),
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

    private ArraySchema Array(JsonElement schema, ImmutableSortedDictionary<string, JsonElement> properties, string? containingNamespace)
    {
        var itemsSchema = schema.GetRequiredProperty("items");

        var items = Schema(itemsSchema, containingNamespace);

        return new ArraySchema(items, properties);
    }

    private MapSchema Map(JsonElement schema, ImmutableSortedDictionary<string, JsonElement> properties, string? containingNamespace)
    {
        var valuesSchema = schema.GetRequiredProperty("values");

        var values = Schema(valuesSchema, containingNamespace);

        return new MapSchema(values, properties);
    }

    private EnumSchema Enum(JsonElement schema, ImmutableSortedDictionary<string, JsonElement> properties, string? containingNamespace)
    {
        var schemaName = schema.GetRequiredSchemaName(containingNamespace);

        if (_schemas.ContainsKey(schemaName))
            throw new InvalidSchemaException($"Redeclaration of schema '{schemaName}'");

        var documentation = schema.GetDocumentation();
        var aliases = schema.GetAliases();
        var symbols = schema.GetSymbols();
        var @default = schema.GetNullableString("default");

        var enumSchema = new EnumSchema(schema, schemaName, documentation, aliases, symbols, @default, properties);
        _schemas[schemaName] = enumSchema;

        return enumSchema;
    }

    private RecordSchema Record(JsonElement schema, ImmutableSortedDictionary<string, JsonElement> properties, string? containingNamespace)
    {
        var schemaName = schema.GetRequiredSchemaName(containingNamespace);

        if (_schemas.ContainsKey(schemaName))
            throw new InvalidSchemaException($"Redeclaration of schema '{schemaName}'");

        var documentation = schema.GetDocumentation();
        var aliases = schema.GetAliases();
        var fields = Fields(schema, schemaName.Namespace);

        var recordSchema = new RecordSchema(schema, schemaName, documentation, aliases, fields, properties);

        _schemas[schemaName] = recordSchema;

        return recordSchema;
    }

    private ErrorSchema Error(JsonElement schema, ImmutableSortedDictionary<string, JsonElement> properties, string? containingNamespace)
    {
        var schemaName = schema.GetRequiredSchemaName(containingNamespace);

        if (_schemas.ContainsKey(schemaName))
            throw new InvalidSchemaException($"Redeclaration of schema '{schemaName}'");

        var documentation = schema.GetDocumentation();
        var aliases = schema.GetAliases();
        var fields = Fields(schema, schemaName.Namespace);

        var errorSchema = new ErrorSchema(schema, schemaName, documentation, aliases, fields, properties);
        _schemas[schemaName] = errorSchema;

        return errorSchema;
    }

    private ImmutableArray<Field> Fields(JsonElement schema, string? containingNamespace)
    {
        var fields = ImmutableArray.CreateBuilder<Field>();
        foreach (var field in schema.GetRequiredArray("fields"))
            fields.Add(Field(field, containingNamespace));

        return fields.ToImmutable();
    }

    private Field Field(JsonElement field, string? containingNamespace)
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
        var aliases = field.GetAliases();
        var defaultJson = field.GetNullableProperty("default");
        var @default = GetValue(type, defaultJson);
        var order = field.GetNullableInt32("order");
        return new Field(name, type, underlyingType, isNullable, documentation, aliases, defaultJson, @default, order);
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
            "float" => GetFloatValue(value),
            "double" => value.GetRawText(),
            "byte[]" => GetBytesValue(value),
            "string" => value.GetRawText(),
            _ when _schemas.TryGetValue(type.SchemaName, out var namedSchema) && namedSchema.Type is SchemaType.Enum => $"{type}.{value.GetString()}",

            // TODO: Do we need to handle complex types? Should they be supported?
            _ => null,

        };

        static string GetFloatValue(JsonElement value)
        {
            var text = value.GetRawText();
            return text.Contains('.') ? $"{text}f" : text;
        }

        static string GetBytesValue(JsonElement value)
        {
            var text = value.GetString();
            var bytes = Encoding.UTF8.GetBytes(text);
            return $"[{string.Join(", ", bytes.Select(bytes => $"0x{bytes:X2}"))}]";
        }
    }

    private FixedSchema Fixed(JsonElement schema, ImmutableSortedDictionary<string, JsonElement> properties, string? containingNamespace)
    {
        var schemaName = schema.GetRequiredSchemaName(containingNamespace);

        if (_schemas.ContainsKey(schemaName))
            throw new InvalidSchemaException($"Redeclaration of schema '{schemaName}'");

        var documentation = schema.GetDocumentation();
        var aliases = schema.GetAliases();
        var size = schema.GetFixedSize();

        var fixedSchema = new FixedSchema(schema, schemaName, documentation, aliases, size, properties);
        _schemas[schemaName] = fixedSchema;

        return fixedSchema;
    }

    private AvroSchema Logical(JsonElement schema, ImmutableSortedDictionary<string, JsonElement> properties, string? containingNamespace)
    {
        var logicalType = schema.GetRequiredString("logicalType");

        var underlyingType = schema.GetRequiredString("type");
        var underlyingSchema = underlyingType switch
        {
            "null" => AvroSchema.Object,
            "boolean" => AvroSchema.Boolean,
            "int" => AvroSchema.Int,
            "long" => AvroSchema.Long,
            "float" => AvroSchema.Float,
            "double" => AvroSchema.Double,
            "bytes" => AvroSchema.Bytes,
            "string" => AvroSchema.String,
            "array" => Array(schema, ImmutableSortedDictionary<string, JsonElement>.Empty, containingNamespace),
            "map" => Map(schema, ImmutableSortedDictionary<string, JsonElement>.Empty, containingNamespace),
            "enum" => Enum(schema, ImmutableSortedDictionary<string, JsonElement>.Empty, containingNamespace),
            "record" => Record(schema, ImmutableSortedDictionary<string, JsonElement>.Empty, containingNamespace),
            "error" => Error(schema, ImmutableSortedDictionary<string, JsonElement>.Empty, containingNamespace),
            "fixed" => Fixed(schema, ImmutableSortedDictionary<string, JsonElement>.Empty, containingNamespace),
            _ when _schemas.TryGetValue(new SchemaName(JsonElementExtensions.GetValidName(underlyingType), containingNamespace), out var registeredSchema) => registeredSchema,
            _ => throw new InvalidSchemaException($"Unknown schema type '{underlyingType}' in {schema.GetRawText()}")
        };

        return logicalType switch
        {
            "date" => new LogicalSchema(underlyingSchema, new CSharpName("DateTime", "System"), new SchemaName(logicalType), properties),
            "decimal" => new LogicalSchema(underlyingSchema, new CSharpName("AvroDecimal", "Avro"), new SchemaName(logicalType), properties),
            "duration" => new LogicalSchema(underlyingSchema, underlyingSchema.CSharpName, new SchemaName(logicalType), properties),
            "time-micros" => new LogicalSchema(underlyingSchema, new CSharpName("TimeSpan", "System"), new SchemaName(logicalType), properties),
            "time-millis" => new LogicalSchema(underlyingSchema, new CSharpName("TimeSpan", "System"), new SchemaName(logicalType), properties),
            "timestamp-micros" => new LogicalSchema(underlyingSchema, new CSharpName("DateTime", "System"), new SchemaName(logicalType), properties),
            "timestamp-millis" => new LogicalSchema(underlyingSchema, new CSharpName("DateTime", "System"), new SchemaName(logicalType), properties),
            "local-timestamp-micros" => new LogicalSchema(underlyingSchema, new CSharpName("DateTime", "System"), new SchemaName(logicalType), properties),
            "local-timestamp-millis" => new LogicalSchema(underlyingSchema, new CSharpName("DateTime", "System"), new SchemaName(logicalType), properties),
            "uuid" => new LogicalSchema(underlyingSchema, new CSharpName("Guid", "System"), new SchemaName(logicalType), properties),
            _ => throw new InvalidSchemaException($"Unsupported logical type '{logicalType}' in schema: {schema.GetRawText()}"),
        };
    }

    private UnionSchema Union(JsonElement schema, string? containingNamespace)
    {
        var builder = ImmutableArray.CreateBuilder<AvroSchema>();
        foreach (var innerSchema in schema.EnumerateArray())
            builder.Add(Schema(innerSchema, containingNamespace));

        var schemas = builder.ToImmutable();
        var isNullable = schemas.Any(static schema => schema.Type == SchemaType.Null);
        var underlyingSchema = GetUnderlyingSchema(schemas);
        while (underlyingSchema is UnionSchema union)
            underlyingSchema = GetUnderlyingSchema(union.Schemas);
        var hasQuestionMark = isNullable && (useNullableReferenceTypes || MapsToValueType(underlyingSchema.Type));
        var csharpName = new CSharpName(
            underlyingSchema.CSharpName.Name + (hasQuestionMark ? "?" : ""),
            underlyingSchema.CSharpName.Namespace);

        return new UnionSchema(csharpName, schemas, underlyingSchema, isNullable);

        static AvroSchema GetUnderlyingSchema(ImmutableArray<AvroSchema> schemas)
        {
            return schemas.Length switch
            {
                // T1
                1 => schemas[0],
                // T1 | T2
                2 => (schemas[0].Type, schemas[1].Type) switch
                {
                    // "null" | "null"
                    (SchemaType.Null, SchemaType.Null) => AvroSchema.Object,
                    // "null" | T
                    (SchemaType.Null, _) => schemas[1],
                    // T | "null"
                    (_, SchemaType.Null) => schemas[0],
                    // T1 | T2
                    _ => AvroSchema.Object,
                },
                // T1 | T2 | ... | Tn
                _ => AvroSchema.Object,
            };
        }

        static bool MapsToValueType(SchemaType type) => type switch
        {
            SchemaType.Boolean => true,
            SchemaType.Int => true,
            SchemaType.Long => true,
            SchemaType.Float => true,
            SchemaType.Double => true,
            SchemaType.Enum => true,
            _ => false,
        };
    }
}
