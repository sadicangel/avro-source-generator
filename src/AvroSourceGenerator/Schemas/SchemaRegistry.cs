using System.Collections;
using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed class SchemaRegistry(bool useNullableReferenceTypes)
    : IEnumerable<AvroSchema>
{
    private readonly Dictionary<QualifiedName, AvroSchema> _schemas = [];

    public IEnumerator<AvroSchema> GetEnumerator() => _schemas.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public AvroSchema Register(JsonElement schema)
    {
        var name = Type(schema, containingNamespace: null, nullable: false);

        // The schema will only be registered if it's a named schema.
        return _schemas[name];
    }

    private QualifiedName Type(JsonElement schema, string? containingNamespace, bool nullable)
    {
        return schema.ValueKind switch
        {
            JsonValueKind.String => KnownType(schema, containingNamespace, nullable),
            JsonValueKind.Object => ComplexType(schema, containingNamespace, nullable),
            JsonValueKind.Array => Union(schema, containingNamespace),
            _ => throw new InvalidSchemaException($"Invalid schema: {schema.GetRawText()}")
        };
    }

    private QualifiedName KnownType(JsonElement schema, string? containingNamespace, bool nullable)
    {
        var type = schema.GetString()!;

        return type switch
        {
            "null" => QualifiedName.Object(useNullableReferenceTypes & nullable),
            "boolean" => QualifiedName.Boolean(nullable),
            "int" => QualifiedName.Int(nullable),
            "long" => QualifiedName.Long(nullable),
            "float" => QualifiedName.Float(nullable),
            "double" => QualifiedName.Double(nullable),
            "bytes" => QualifiedName.Bytes(useNullableReferenceTypes & nullable),
            "string" => QualifiedName.String(useNullableReferenceTypes & nullable),
            _ when _schemas.TryGetValue(new QualifiedName(JsonElementExtensions.GetValid(type), containingNamespace), out var registeredType) => registeredType.QualifiedName,
            _ => throw new InvalidSchemaException($"Unknown schema '{type}' in {schema.GetRawText()}")
        };
    }

    private QualifiedName ComplexType(JsonElement schema, string? containingNamespace, bool nullable)
    {
        if (schema.IsSupportedLogicalSchema())
        {
            return Logical(schema, nullable);
        }

        var type = schema.GetSchemaTypeString();

        return type switch
        {
            "array" => Array(schema, containingNamespace, nullable),
            "map" => Map(schema, containingNamespace, nullable),
            "enum" => Enum(schema, containingNamespace, nullable),
            "record" => Record(schema, containingNamespace, nullable),
            "error" => Error(schema, containingNamespace, nullable),
            "fixed" => Fixed(schema, containingNamespace, nullable),
            _ => throw new InvalidSchemaException($"Unknown schema '{type}' in {schema.GetRawText()}")
        };
    }

    private QualifiedName Array(JsonElement schema, string? containingNamespace, bool nullable)
    {
        var itemsSchema = schema.GetSchemaItems();

        var items = Type(itemsSchema, containingNamespace, nullable: false);

        return QualifiedName.Array(items, nullable);
    }

    private QualifiedName Map(JsonElement schema, string? containingNamespace, bool nullable)
    {
        var valuesSchema = schema.GetSchemaValues();

        var values = Type(valuesSchema, containingNamespace, nullable: false);

        return QualifiedName.Map(values, nullable);
    }

    private QualifiedName Enum(JsonElement schema, string? containingNamespace, bool nullable)
    {
        var localName = schema.GetLocalName();
        var @namespace = schema.GetNamespace() ?? containingNamespace;
        var name = new QualifiedName(localName, @namespace);

        if (!_schemas.ContainsKey(name))
        {
            var documentation = schema.GetDocumentation();
            var aliases = schema.GetAliases();
            var symbols = schema.GetSymbols();
            var @default = GetValue(symbols, schema.GetSchemaDefault());

            _schemas[name] = new EnumSchema(schema, name, documentation, aliases, symbols, @default);
        }

        return nullable ? name.ToNullable() : name;

        static string? GetValue(ImmutableArray<string> symbols, JsonElement json)
        {
            if (json.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return null;
            }

            if (json.ValueKind is not JsonValueKind.String)
            {
                // TODO: Allow ignoring invalid non-required properties.
                throw new InvalidSchemaException($"'default' must be a string in schema: {json.GetRawText()}");
            }

            var value = json.GetString();
            if (symbols.IndexOf(value!) == -1)
            {
                throw new InvalidSchemaException($"Default value '{value}' not found in enum symbols: {string.Join(", ", symbols)}");
            }

            return value;
        }
    }

    private QualifiedName Record(JsonElement schema, string? containingNamespace, bool nullable)
    {
        var localName = schema.GetLocalName();
        var @namespace = schema.GetNamespace() ?? containingNamespace;
        var name = new QualifiedName(localName, @namespace);

        if (!_schemas.ContainsKey(name))
        {
            var documentation = schema.GetDocumentation();
            var aliases = schema.GetAliases();
            var fields = Fields(schema, @namespace);

            _schemas[name] = new RecordSchema(schema, name, documentation, aliases, fields);
        }

        return useNullableReferenceTypes & nullable ? name.ToNullable() : name;
    }

    private QualifiedName Error(JsonElement schema, string? containingNamespace, bool nullable)
    {
        var localName = schema.GetLocalName();
        var @namespace = schema.GetNamespace() ?? containingNamespace;
        var name = new QualifiedName(localName, @namespace);

        if (!_schemas.ContainsKey(name))
        {
            var documentation = schema.GetDocumentation();
            var aliases = schema.GetAliases();
            var fields = Fields(schema, @namespace);

            _schemas[name] = new ErrorSchema(schema, name, documentation, aliases, fields);
        }

        return useNullableReferenceTypes & nullable ? name.ToNullable() : name;
    }

    private ImmutableArray<Field> Fields(JsonElement schema, string? containingNamespace) =>
        [.. schema.GetSchemaFields().Select(field => Field(field, containingNamespace))];

    private Field Field(JsonElement field, string? containingNamespace)
    {
        var name = field.GetLocalName();
        var type = Type(field.GetSchemaType(), containingNamespace, nullable: false);
        var documentation = field.GetDocumentation();
        var aliases = field.GetAliases();
        var @default = GetValue(type, field.GetSchemaDefault());
        var order = field.GetFieldOrder();
        return new Field(name, type, documentation, aliases, @default, order);
    }

    private string? GetValue(QualifiedName type, JsonElement value)
    {
        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        // TODO: Actually validate the value so that we don't generate invalid code.
        return type.LocalName switch
        {
            "object" or "object?" => null,
            "bool" or "bool?" => value.GetRawText(),
            "int" or "int?" => value.GetRawText(),
            "long" or "long?" => value.GetRawText(),
            "float" or "float?" => value.GetRawText(),
            "double" or "double?" => value.GetRawText(),
            "bytes" or "bytes?" => value.GetRawText(),
            "array" or "array?" => value.GetRawText(),
            "map" or "map?" => value.GetRawText(),
            "string" or "string?" => value.GetRawText(),
            _ when _schemas.TryGetValue(type, out var namedSchema) && namedSchema.Type is SchemaType.Enum => $"{type}.{value.GetString()}",

            // TODO: Do we need to handle complex types? Should they be supported?
            _ => null,
        };
    }

    private QualifiedName Fixed(JsonElement schema, string? containingNamespace, bool nullable)
    {
        var localName = schema.GetLocalName();
        var @namespace = schema.GetNamespace() ?? containingNamespace;
        var name = new QualifiedName(localName, @namespace);

        if (!_schemas.ContainsKey(name))
        {
            var documentation = schema.GetDocumentation();
            var aliases = schema.GetAliases();
            var size = schema.GetFixedSize();

            _schemas[name] = new FixedSchema(schema, name, documentation, aliases, size);
        }

        return useNullableReferenceTypes & nullable ? name.ToNullable() : name;
    }

    private QualifiedName Logical(JsonElement schema, bool nullable)
    {
        var logicalType = schema.GetLogicalType();

        return logicalType switch
        {
            "date" => QualifiedName.Date(nullable),
            "decimal" => QualifiedName.Decimal(nullable),
            // "duration" => handled as a fixed type,
            "time-micros" => QualifiedName.TimeMicros(nullable),
            "time-millis" => QualifiedName.TimeMillis(nullable),
            "timestamp-micros" => QualifiedName.TimestampMicros(nullable),
            "timestamp-millis" => QualifiedName.TimestampMillis(nullable),
            "local-timestamp-micros" => QualifiedName.LocalTimestampMicros(nullable),
            "local-timestamp-millis" => QualifiedName.LocalTimestampMillis(nullable),
            "uuid" => QualifiedName.Uuid(nullable),
            _ => throw new InvalidSchemaException($"Unsupported logical type '{logicalType}' in schema: {schema.GetRawText()}"),
        };
    }

    private QualifiedName Union(JsonElement schema, string? containingNamespace)
    {
        var length = schema.GetArrayLength();

        return length switch
        {
            1 => Type(schema[0], containingNamespace, nullable: false),
            2 => (JsonElementExtensions.IsNullSchema(schema[0]), JsonElementExtensions.IsNullSchema(schema[1])) switch
            {
                // "null" | "null"
                (true, true) => QualifiedName.Object(useNullableReferenceTypes),
                // "null" | T
                (true, false) => Type(schema[1], containingNamespace, nullable: true),
                // T | "null"
                (false, true) => Type(schema[0], containingNamespace, nullable: true),
                // T1 | T2
                _ => QualifiedName.Object(nullable: false),
            },
            // T1 | T2 | ... | Tn
            _ => QualifiedName.Object(useNullableReferenceTypes & schema.EnumerateArray().Any(JsonElementExtensions.IsNullSchema)),
        };
    }
}
