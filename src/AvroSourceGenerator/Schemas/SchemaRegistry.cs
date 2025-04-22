using System.Collections;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using AvroSourceGenerator.Schemas.Extensions;

namespace AvroSourceGenerator.Schemas;

internal readonly struct SchemaRegistry(bool useNullableReferenceTypes) : IReadOnlyCollection<NamedSchema>
{
    private readonly record struct SchemaKey(string Name, string? Namespace);

    private readonly Dictionary<SchemaKey, NamedSchema> _schemas = [];

    public int Count => _schemas.Count;

    public IEnumerator<NamedSchema> GetEnumerator() => _schemas.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static SchemaRegistry Register(JsonElement schema, bool useNullableReferenceTypes)
    {
        var registry = new SchemaRegistry(useNullableReferenceTypes);

        var namedSchema = registry.Schema(schema, containingNamespace: null);

        // The schema will only be registered if it's a named schema.
        if (namedSchema is not NamedSchema)
        {
            throw new InvalidSchemaException($"Schema is not a named schema: {schema.GetRawText()}");
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
        var type = schema.GetString()!;

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
            _ when _schemas.TryGetValue(new SchemaKey(JsonElementExtensions.GetValidName(type), containingNamespace), out var registeredSchema) => registeredSchema,
            _ => throw new InvalidSchemaException($"Unknown schema '{type}' in {schema.GetRawText()}")
        };
    }

    private AvroSchema Complex(JsonElement schema, string? containingNamespace)
    {
        if (schema.IsSupportedLogicalSchema())
        {
            return Logical(schema);
        }

        var type = schema.GetSchemaType();

        return type switch
        {
            "array" => Array(schema, containingNamespace),
            "map" => Map(schema, containingNamespace),
            "enum" => Enum(schema, containingNamespace),
            "record" => Record(schema, containingNamespace),
            "error" => Error(schema, containingNamespace),
            "fixed" => Fixed(schema, containingNamespace),
            _ => throw new InvalidSchemaException($"Unknown schema '{type}' in {schema.GetRawText()}")
        };
    }

    private ArraySchema Array(JsonElement schema, string? containingNamespace)
    {
        var itemsSchema = schema.GetRequiredProperty("items");

        var items = Schema(itemsSchema, containingNamespace);

        return new ArraySchema(items);
    }

    private MapSchema Map(JsonElement schema, string? containingNamespace)
    {
        var valuesSchema = schema.GetRequiredProperty("values");

        var values = Schema(valuesSchema, containingNamespace);

        return new MapSchema(values);
    }

    private EnumSchema Enum(JsonElement schema, string? containingNamespace)
    {
        var name = schema.GetFullName(out var @namespace);
        @namespace ??= containingNamespace;
        var schemaKey = new SchemaKey(name, @namespace);

        if (_schemas.ContainsKey(schemaKey))
            throw new InvalidSchemaException($"Redeclaration of schema '{name}' in namespace '{@namespace}'");

        var documentation = schema.GetDocumentation();
        var aliases = schema.GetAliases();
        var symbols = schema.GetSymbols();
        var @default = schema.GetNullableString("default");

        var enumSchema = new EnumSchema(schema, name, @namespace, documentation, aliases, symbols, @default);
        _schemas[schemaKey] = enumSchema;

        return enumSchema;
    }

    private RecordSchema Record(JsonElement schema, string? containingNamespace)
    {
        var name = schema.GetFullName(out var @namespace);
        @namespace ??= containingNamespace;
        var schemaKey = new SchemaKey(name, @namespace);

        if (_schemas.ContainsKey(schemaKey))
            throw new InvalidSchemaException($"Redeclaration of schema '{name}' in namespace '{@namespace}'");

        var documentation = schema.GetDocumentation();
        var aliases = schema.GetAliases();
        var fields = Fields(schema, @namespace);

        var recordSchema = new RecordSchema(schema, name, @namespace, documentation, aliases, fields);

        _schemas[schemaKey] = recordSchema;

        return recordSchema;
    }

    private ErrorSchema Error(JsonElement schema, string? containingNamespace)
    {
        var name = schema.GetFullName(out var @namespace);
        @namespace ??= containingNamespace;
        var schemaKey = new SchemaKey(name, @namespace);

        if (_schemas.ContainsKey(schemaKey))
            throw new InvalidSchemaException($"Redeclaration of schema '{name}' in namespace '{@namespace}'");

        var documentation = schema.GetDocumentation();
        var aliases = schema.GetAliases();
        var fields = Fields(schema, @namespace);

        var errorSchema = new ErrorSchema(schema, name, @namespace, documentation, aliases, fields);
        _schemas[schemaKey] = errorSchema;

        return errorSchema;
    }

    private ImmutableArray<Field> Fields(JsonElement schema, string? containingNamespace)
    {
        var fields = ImmutableArray.CreateBuilder<Field>();
        foreach (var field in schema.GetRequiredArray("fields"))
        {
            fields.Add(Field(field, containingNamespace));
        }

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
        var @default = GetValue(type, field.GetNullableProperty("default"));
        var order = field.GetNullableInt32("order");
        return new Field(name, type, underlyingType, isNullable, documentation, aliases, @default, order);
    }

    private string? GetValue(AvroSchema type, JsonElement? json)
    {
        if (json is null or { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined })
        {
            return null;
        }

        var value = json.Value;

        // TODO: Actually validate the value so that we don't generate invalid code.
        return type.Name switch
        {
            "object" => value.GetRawText(),
            "bool" => value.GetRawText(),
            "int" => value.GetRawText(),
            "long" => value.GetRawText(),
            "float" => GetFloatValue(value),
            "double" => value.GetRawText(),
            "byte[]" => GetBytesValue(value),
            "string" => value.GetRawText(),
            _ when _schemas.TryGetValue(new SchemaKey(type.Name, type.Namespace), out var namedSchema) && namedSchema.Type is SchemaType.Enum => $"{type}.{value.GetString()}",

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

    private FixedSchema Fixed(JsonElement schema, string? containingNamespace)
    {
        var name = schema.GetFullName(out var @namespace);
        @namespace ??= containingNamespace;
        var schemaKey = new SchemaKey(name, @namespace);

        if (_schemas.ContainsKey(schemaKey))
            throw new InvalidSchemaException($"Redeclaration of schema '{name}' in namespace '{@namespace}'");

        var documentation = schema.GetDocumentation();
        var aliases = schema.GetAliases();
        var size = schema.GetFixedSize();

        var fixedSchema = new FixedSchema(schema, name, @namespace, documentation, aliases, size);
        _schemas[schemaKey] = fixedSchema;

        return fixedSchema;
    }

    private static AvroSchema Logical(JsonElement schema)
    {
        var logicalType = schema.GetRequiredString("logicalType");

        return logicalType switch
        {
            "date" => AvroSchema.Date,
            "decimal" => AvroSchema.Decimal,
            // "duration" => handled as a fixed type,
            "time-micros" => AvroSchema.TimeMicros,
            "time-millis" => AvroSchema.TimeMillis,
            "timestamp-micros" => AvroSchema.TimestampMicros,
            "timestamp-millis" => AvroSchema.TimestampMillis,
            "local-timestamp-micros" => AvroSchema.LocalTimestampMicros,
            "local-timestamp-millis" => AvroSchema.LocalTimestampMillis,
            "uuid" => AvroSchema.Uuid,
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
        var name = underlyingSchema.Name + (hasQuestionMark ? "?" : "");
        var @namespace = underlyingSchema.Namespace;

        return new UnionSchema(name, @namespace, schemas, underlyingSchema, isNullable);

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
