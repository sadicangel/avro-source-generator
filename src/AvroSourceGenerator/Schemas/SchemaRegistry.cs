using System.Collections;
using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed class SchemaRegistry : IEnumerable<AvroSchema>, IDisposable
{
    private readonly Dictionary<QualifiedName, AvroSchema> _schemas = [];
    private readonly JsonDocument _rootSchema;
    private readonly LanguageFeatures _languageFeatures;

    public SchemaRegistry(JsonDocument rootSchema, LanguageFeatures languageFeatures)
    {
        _rootSchema = rootSchema;
        _languageFeatures = languageFeatures;

        Type(rootSchema.RootElement, null, nullable: false);
    }

    private bool UseNullableReferenceTypes => _languageFeatures.HasFlag(LanguageFeatures.NullableReferenceTypes);

    public IEnumerator<AvroSchema> GetEnumerator() => _schemas.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Dispose() => _rootSchema.Dispose();

    private QualifiedName Type(JsonElement schema, string? @namespace, bool nullable)
    {
        return schema.ValueKind switch
        {
            JsonValueKind.String => KnownType(schema, @namespace, nullable),
            JsonValueKind.Object => ComplexType(schema, @namespace, nullable),
            JsonValueKind.Array => Union(schema, @namespace),
            _ => throw new InvalidOperationException($"Invalid schema {schema.GetRawText()}")
        };
    }

    private QualifiedName KnownType(JsonElement schema, string? @namespace, bool nullable)
    {
        // TODO: Find a way to avoid string allocation here. Maybe go back to use internal raw value?
        var name = schema.GetString() ?? throw new InvalidOperationException($"Invalid schema {schema.GetRawText()}");

        return name switch
        {
            "null" => QualifiedName.Object(UseNullableReferenceTypes & nullable),
            "boolean" => QualifiedName.Boolean(nullable),
            "int" => QualifiedName.Int(nullable),
            "long" => QualifiedName.Long(nullable),
            "float" => QualifiedName.Float(nullable),
            "double" => QualifiedName.Double(nullable),
            "bytes" => QualifiedName.Bytes(UseNullableReferenceTypes & nullable),
            "string" => QualifiedName.String(UseNullableReferenceTypes & nullable),
            _ when _schemas.TryGetValue(new QualifiedName(name, @namespace), out var registeredType) => registeredType.QualifiedName,
            _ => throw new InvalidOperationException($"Invalid schema {schema.GetRawText()}")
        };
    }

    private QualifiedName ComplexType(JsonElement schema, string? @namespace, bool nullable)
    {
        if (schema.TryGetProperty("logicalType", out var logicalType))
            return Logical(logicalType, nullable);

        // TODO: Find a way to avoid string allocation here. Maybe go back to use internal raw value?
        var type = schema.GetProperty("type").GetString() ?? throw new InvalidOperationException($"Invalid schema {schema.GetRawText()}");

        return type switch
        {
            "array" => Array(schema, @namespace, nullable),
            "map" => Map(schema, @namespace, nullable),
            "enum" => Enum(schema, @namespace, nullable),
            "record" => Record(schema, @namespace, nullable),
            "error" => Error(schema, @namespace, nullable),
            "fixed" => Fixed(schema, @namespace, nullable),
            _ => throw new InvalidOperationException($"Invalid schema {schema.GetRawText()}")
        };
    }

    private QualifiedName Array(JsonElement schema, string? @namespace, bool nullable)
    {
        var items = Type(schema.GetProperty("items"), @namespace, nullable: false);

        return QualifiedName.Array(items, nullable);
    }

    private QualifiedName Map(JsonElement schema, string? @namespace, bool nullable)
    {
        var values = Type(schema.GetProperty("values"), @namespace, nullable: false);

        return QualifiedName.Map(values, nullable);
    }

    private QualifiedName Enum(JsonElement schema, string? @namespace, bool nullable)
    {
        var localName = NameHelper.GetLocalName(schema);
        @namespace = NameHelper.GetNamespace(schema) ?? @namespace;
        var name = new QualifiedName(localName, @namespace);

        if (!_schemas.TryGetValue(name, out var enumSchema))
        {
            var documentation = schema.TryGetProperty("doc", out var doc) ? doc.GetString() : null;
            var aliases = schema.TryGetProperty("aliases", out var ali) ? ali.EnumerateArray().Select(alias => alias.GetString() ?? throw new InvalidOperationException($"Invalid schema {schema.GetRawText()}")).ToImmutableArray() : [];
            var symbols = schema.GetProperty("symbols").EnumerateArray().Select(symbol => NameHelper.GetValid(symbol.GetString() ?? throw new InvalidOperationException($"Invalid schema {schema.GetRawText()}"))).ToImmutableArray();
            var @default = schema.TryGetProperty("default", out var def) ? def.GetString() : null;
            if (@default is not null && symbols.IndexOf(@default) is -1)
                throw new InvalidOperationException($"Invalid schema {schema.GetRawText()}");

            _schemas[name] = enumSchema = new EnumSchema(name, documentation, aliases, symbols, @default);
        }

        return nullable ? name.ToNullable() : name;

    }

    private QualifiedName Record(JsonElement schema, string? @namespace, bool nullable)
    {
        var localName = NameHelper.GetLocalName(schema);
        @namespace = NameHelper.GetNamespace(schema) ?? @namespace;
        var name = new QualifiedName(localName, @namespace);

        if (!_schemas.TryGetValue(name, out var recordSchema))
        {
            var documentation = schema.TryGetProperty("doc", out var doc) ? doc.GetString() : null;
            var aliases = schema.TryGetProperty("aliases", out var ali) ? ali.EnumerateArray().Select(alias => alias.GetString() ?? throw new InvalidOperationException($"Invalid schema {schema.GetRawText()}")).ToImmutableArray() : [];
            var fields = schema.GetProperty("fields").EnumerateArray().Select(field => Field(field, @namespace)).ToImmutableArray();

            _schemas[name] = recordSchema = new RecordSchema(name, documentation, aliases, fields);
        }

        return UseNullableReferenceTypes & nullable ? name.ToNullable() : name;
    }

    private QualifiedName Error(JsonElement schema, string? @namespace, bool nullable)
    {
        var localName = NameHelper.GetLocalName(schema);
        @namespace = NameHelper.GetNamespace(schema) ?? @namespace;
        var name = new QualifiedName(localName, @namespace);

        if (!_schemas.TryGetValue(name, out var recordSchema))
        {
            var documentation = schema.TryGetProperty("doc", out var doc) ? doc.GetString() : null;
            var aliases = schema.TryGetProperty("aliases", out var ali) ? ali.EnumerateArray().Select(alias => alias.GetString() ?? throw new InvalidOperationException($"Invalid schema {schema.GetRawText()}")).ToImmutableArray() : [];
            var fields = schema.GetProperty("fields").EnumerateArray().Select(field => Field(field, @namespace)).ToImmutableArray();

            _schemas[name] = recordSchema = new ErrorSchema(name, documentation, aliases, fields);
        }

        return UseNullableReferenceTypes & nullable ? name.ToNullable() : name;
    }

    private Field Field(JsonElement field, string? @namespace)
    {
        var name = NameHelper.GetLocalName(field);
        var type = Type(field.GetProperty("type"), @namespace, nullable: false);
        var documentation = field.TryGetProperty("doc", out var doc) ? doc.GetString() : null;
        var aliases = field.TryGetProperty("aliases", out var ali) ? ali.EnumerateArray().Select(alias => alias.GetString() ?? throw new InvalidOperationException($"Invalid field {field.GetRawText()}")).ToImmutableArray() : [];
        var @default = field.TryGetProperty("default", out var def) ? GetValue(type, def) : null;
        var order = field.TryGetProperty("order", out var ord) ? ord.GetInt32() : default(int?);
        return new Field(name, type, documentation, aliases, @default, order);
    }

    private string? GetValue(QualifiedName type, JsonElement value)
    {
        if (value.ValueKind is JsonValueKind.Null)
        {
            return null;
        }

        // TODO: Actually validate the value so that we don't generate invalid code.
        return type.Name switch
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
            "string" or "string?" => value.GetString(),
            _ when _schemas.TryGetValue(type, out var namedSchema) && namedSchema.Type is SchemaType.Enum => value.GetString(),

            // TODO: Do we need to handle complex types? Should they be supported?
            _ => null,
        };
    }

    private QualifiedName Fixed(JsonElement schema, string? @namespace, bool nullable)
    {
        var localName = NameHelper.GetLocalName(schema);
        @namespace = NameHelper.GetNamespace(schema) ?? @namespace;
        var name = new QualifiedName(localName, @namespace);

        if (!_schemas.TryGetValue(name, out var fixedSchema))
        {
            var documentation = schema.TryGetProperty("doc", out var doc) ? doc.GetString() : null;
            var aliases = schema.TryGetProperty("aliases", out var ali) ? ali.EnumerateArray().Select(alias => alias.GetString() ?? throw new InvalidOperationException($"Invalid schema {schema.GetRawText()}")).ToImmutableArray() : [];
            var size = schema.GetProperty("size").GetInt32();

            _schemas[name] = fixedSchema = new FixedSchema(name, documentation, aliases, size);
        }

        return UseNullableReferenceTypes & nullable ? name.ToNullable() : name;
    }

    private QualifiedName Logical(JsonElement schema, bool nullable)
    {
        // TODO: Find a way to avoid string allocation here. Maybe go back to use internal raw value?
        var logicalType = schema.GetString() ?? throw new InvalidOperationException($"Invalid schema {schema.GetRawText()}");

        return logicalType switch
        {
            "date" => QualifiedName.Date(nullable),
            "decimal" => QualifiedName.Decimal(nullable),
            "duration" => QualifiedName.Duration(UseNullableReferenceTypes & nullable),
            "local-timestamp-micros" => QualifiedName.LocalTimestampMicros(nullable),
            "local-timestamp-millis" => QualifiedName.LocalTimestampMillis(nullable),
            "time-micros" => QualifiedName.TimeMicros(nullable),
            "time-millis" => QualifiedName.TimeMillis(nullable),
            "timestamp-micros" => QualifiedName.TimestampMicros(nullable),
            "timestamp-millis" => QualifiedName.TimestampMillis(nullable),
            "uuid" => QualifiedName.Uuid(nullable),
            _ => throw new NotSupportedException(schema.GetRawText()),
        };
    }

    private QualifiedName Union(JsonElement schema, string? @namespace)
    {
        var length = schema.GetArrayLength();

        return length switch
        {
            1 => Type(schema[0], @namespace, nullable: false),
            2 => (IsNull(schema[0]), IsNull(schema[1])) switch
            {
                // "null" | "null"
                (true, true) => QualifiedName.Object(UseNullableReferenceTypes),
                // "null" | T
                (true, false) => Type(schema[1], @namespace, nullable: true),
                // T | "null"
                (false, true) => Type(schema[0], @namespace, nullable: true),
                // T1 | T2
                _ => QualifiedName.Object(nullable: false),
            },
            // T1 | T2 | ... | Tn
            _ => QualifiedName.Object(UseNullableReferenceTypes & schema.EnumerateArray().Any(IsNull)),
        };

        // TODO: Find a way to avoid string allocation here. Maybe go back to use internal raw value?
        static bool IsNull(JsonElement schema) =>
            schema.ValueKind is JsonValueKind.String && schema.GetString() == "null";
    }
}
