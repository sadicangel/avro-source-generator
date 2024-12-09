using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed class SchemaRegistry(LanguageFeatures languageFeatures, string? namespaceOverride = null)
    : IEnumerable<AvroSchema>
{
    private readonly Dictionary<QualifiedName, AvroSchema> _schemas = [];

    private bool UseNullableReferenceTypes => languageFeatures.HasFlag(LanguageFeatures.NullableReferenceTypes);

    public IEnumerator<AvroSchema> GetEnumerator() => _schemas.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public AvroSchema Register(JsonElement schema)
    {
        var name = Type(schema, @namespace: null, nullable: false);

        // The schema will only be registered if it's a named schema.
        return _schemas[name];
    }

    private QualifiedName Type(JsonElement schema, string? @namespace, bool nullable)
    {
        return schema.ValueKind switch
        {
            JsonValueKind.String => KnownType(schema, @namespace, nullable),
            JsonValueKind.Object => ComplexType(schema, @namespace, nullable),
            JsonValueKind.Array => Union(schema, @namespace),
            _ => throw new InvalidSchemaException($"Invalid schema type: {schema.GetRawText()}")
        };
    }

    private QualifiedName KnownType(JsonElement schema, string? @namespace, bool nullable)
    {
        var name = schema.GetString() ?? throw new InvalidSchemaException($"Invalid schema: expected a string but got {schema.GetRawText()}");

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
            _ when _schemas.TryGetValue(new QualifiedName(Helper.GetValid(name), @namespace), out var registeredType) => registeredType.QualifiedName,
            _ => throw new InvalidSchemaException($"Unknown schema type: {name}")
        };
    }

    private QualifiedName ComplexType(JsonElement schema, string? @namespace, bool nullable)
    {
        if (schema.TryGetProperty("logicalType", out var logicalType))
            return Logical(logicalType, nullable);

        var type = schema.GetProperty("type").GetString() ?? throw new InvalidSchemaException($"Invalid schema: missing 'type' property in {schema.GetRawText()}");

        return type switch
        {
            "array" => Array(schema, @namespace, nullable),
            "map" => Map(schema, @namespace, nullable),
            "enum" => Enum(schema, @namespace, nullable),
            "record" => Record(schema, @namespace, nullable),
            "error" => Error(schema, @namespace, nullable),
            "fixed" => Fixed(schema, @namespace, nullable),
            _ => throw new InvalidSchemaException($"Unknown complex schema type: {type} in {schema.GetRawText()}")
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
        var localName = Helper.GetLocalName(schema);
        @namespace = namespaceOverride ?? Helper.GetNamespace(schema) ?? @namespace;
        var name = new QualifiedName(localName, @namespace);

        if (!_schemas.ContainsKey(name))
        {
            var documentation = Helper.GetDocumentation(schema);
            var aliases = Helper.GetAliases(schema);
            var symbols = schema.GetProperty("symbols").EnumerateArray().Select(symbol => Helper.GetValid(symbol.GetString() ?? throw new InvalidSchemaException($"Invalid symbol in schema: {schema.GetRawText()}"))).ToImmutableArray();
            var @default = schema.TryGetProperty("default", out var def) ? def.GetString() : null;
            if (@default is not null && symbols.IndexOf(@default) is -1)
                throw new InvalidSchemaException($"Default value '{@default}' not found in symbols for schema: {schema.GetRawText()}");

            _schemas[name] = new EnumSchema(schema, name, documentation, aliases, symbols, @default);
        }

        return nullable ? name.ToNullable() : name;
    }

    private QualifiedName Record(JsonElement schema, string? @namespace, bool nullable)
    {
        var localName = Helper.GetLocalName(schema);
        @namespace = namespaceOverride ?? Helper.GetNamespace(schema) ?? @namespace;
        var name = new QualifiedName(localName, @namespace);

        if (!_schemas.ContainsKey(name))
        {
            var documentation = Helper.GetDocumentation(schema);
            var aliases = Helper.GetAliases(schema);
            var fields = schema.GetProperty("fields").EnumerateArray().Select(field => Field(field, @namespace)).ToImmutableArray();

            _schemas[name] = new RecordSchema(schema, name, documentation, aliases, fields);
        }

        return UseNullableReferenceTypes & nullable ? name.ToNullable() : name;
    }

    private QualifiedName Error(JsonElement schema, string? @namespace, bool nullable)
    {
        var localName = Helper.GetLocalName(schema);
        @namespace = namespaceOverride ?? Helper.GetNamespace(schema) ?? @namespace;
        var name = new QualifiedName(localName, @namespace);

        if (!_schemas.ContainsKey(name))
        {
            var documentation = Helper.GetDocumentation(schema);
            var aliases = Helper.GetAliases(schema);
            var fields = schema.GetProperty("fields").EnumerateArray().Select(field => Field(field, @namespace)).ToImmutableArray();

            _schemas[name] = new ErrorSchema(schema, name, documentation, aliases, fields);
        }

        return UseNullableReferenceTypes & nullable ? name.ToNullable() : name;
    }

    private Field Field(JsonElement field, string? @namespace)
    {
        var name = Helper.GetLocalName(field);
        var type = Type(field.GetProperty("type"), @namespace, nullable: false);
        var documentation = Helper.GetDocumentation(field);
        var aliases = Helper.GetAliases(field);
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
        var localName = Helper.GetLocalName(schema);
        @namespace = namespaceOverride ?? Helper.GetNamespace(schema) ?? @namespace;
        var name = new QualifiedName(localName, @namespace);

        if (!_schemas.ContainsKey(name))
        {
            var documentation = Helper.GetDocumentation(schema);
            var aliases = Helper.GetAliases(schema);
            var size = schema.GetProperty("size").GetInt32();

            _schemas[name] = new FixedSchema(schema, name, documentation, aliases, size);
        }

        return UseNullableReferenceTypes & nullable ? name.ToNullable() : name;
    }

    private QualifiedName Logical(JsonElement schema, bool nullable)
    {
        var logicalType = schema.GetString() ?? throw new InvalidSchemaException($"Invalid logical type in schema: {schema.GetRawText()}");

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
            _ => throw new InvalidSchemaException($"Unsupported logical type: {logicalType} in schema: {schema.GetRawText()}"),
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

        // TODO: Avoid allocating a new string to compare against "null".
        static bool IsNull(JsonElement schema) =>
            schema.ValueKind is JsonValueKind.String && schema.GetString() == "null";
    }
}

file static class Helper
{
    public static bool IsReserved(ReadOnlySpan<char> name, [MaybeNullWhen(false)] out string replacement)
    {
        replacement = name switch
        {
            "bool" => "@bool",
            "byte" => "@byte",
            "sbyte" => "@sbyte",
            "short" => "@short",
            "ushort" => "@ushort",
            "int" => "@int",
            "uint" => "@uint",
            "long" => "@long",
            "ulong" => "@ulong",
            "double" => "@double",
            "float" => "@float",
            "decimal" => "@decimal",
            "string" => "@string",
            "char" => "@char",
            "void" => "@void",
            "object" => "@object",
            "typeof" => "@typeof",
            "sizeof" => "@sizeof",
            "null" => "@null",
            "true" => "@true",
            "false" => "@false",
            "if" => "@if",
            "else" => "@else",
            "while" => "@while",
            "for" => "@for",
            "foreach" => "@foreach",
            "do" => "@do",
            "switch" => "@switch",
            "case" => "@case",
            "default" => "@default",
            "try" => "@try",
            "catch" => "@catch",
            "finally" => "@finally",
            "lock" => "@lock",
            "goto" => "@goto",
            "break" => "@break",
            "continue" => "@continue",
            "return" => "@return",
            "throw" => "@throw",
            "public" => "@public",
            "private" => "@private",
            "internal" => "@internal",
            "protected" => "@protected",
            "static" => "@static",
            "readonly" => "@readonly",
            "sealed" => "@sealed",
            "const" => "@const",
            "fixed" => "@fixed",
            "stackalloc" => "@stackalloc",
            "volatile" => "@volatile",
            "new" => "@new",
            "override" => "@override",
            "abstract" => "@abstract",
            "virtual" => "@virtual",
            "event" => "@event",
            "extern" => "@extern",
            "ref" => "@ref",
            "out" => "@out",
            "in" => "@in",
            "is" => "@is",
            "as" => "@as",
            "params" => "@params",
            "__arglist" => "@__arglist",
            "__makeref" => "@__makeref",
            "__reftype" => "@__reftype",
            "__refvalue" => "@__refvalue",
            "this" => "@this",
            "base" => "@base",
            "namespace" => "@namespace",
            "using" => "@using",
            "class" => "@class",
            "struct" => "@struct",
            "interface" => "@interface",
            "enum" => "@enum",
            "delegate" => "@delegate",
            "checked" => "@checked",
            "unchecked" => "@unchecked",
            "unsafe" => "@unsafe",
            "operator" => "@operator",
            "explicit" => "@explicit",
            "implicit" => "@implicit",
            _ => null
        };

        return replacement is not null;
    }

    public static string GetValid(string name) => IsReserved(name, out var replacement) ? replacement : name;

    public static string GetLocalName(JsonElement schema)
    {
        var name = schema.GetProperty("name").GetString();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidSchemaException($"'name' property cannot be null or empty in the schema: {schema.GetRawText()}");
        }
        return GetValid(name!);
    }

    public static string? GetNamespace(JsonElement schema)
    {
        // TODO: We can probably benefit from accessing the raw value directly instead of allocating a string.
        if (!schema.TryGetProperty("namespace", out var json) || json.GetString() is not string @namespace)
            return null;

        var builder = new StringBuilder("global::");

        foreach (var part in new SplitEnumerable(@namespace, '.'))
        {
            if (IsReserved(part, out var replacement))
            {
                builder.Append(replacement);
            }
            else
            {
                builder.EnsureCapacity(builder.Length + part.Length);
                for (var i = 0; i < part.Length; ++i)
                    builder.Append(part[i]);
            }
        }

        return builder.ToString();
    }

    public static string? GetDocumentation(JsonElement schema) =>
        schema.TryGetProperty("doc", out var doc) ? doc.GetString() : null;

    public static ImmutableArray<string> GetAliases(JsonElement schema)
    {
        if (!schema.TryGetProperty("aliases", out var ali) || ali.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return [];
        }

        return ali.EnumerateArray()
            .Select(alias => alias.GetString()
                ?? throw new InvalidSchemaException($"Invalid alias in schema: {schema.GetRawText()}"))
            .ToImmutableArray();
    }

}

file readonly ref struct SplitEnumerable(ReadOnlySpan<char> value, char separator)
{
    private readonly ReadOnlySpan<char> _value = value;
    private readonly char _separator = separator;

    public SplitEnumerator GetEnumerator() => new(_value, _separator);

    public ref struct SplitEnumerator
    {
        private readonly ReadOnlySpan<char> _value;
        private readonly char _separator;
        private int _start;
        public SplitEnumerator(ReadOnlySpan<char> value, char separator)
        {
            _value = value;
            _separator = separator;
            _start = -1;
            Current = default;
        }
        public ReadOnlySpan<char> Current { get; private set; }

        public bool MoveNext()
        {
            if (_start >= _value.Length)
            {
                return false;
            }
            var end = _value[(_start + 1)..].IndexOf(_separator);
            if (end == -1)
            {
                Current = _value[(_start + 1)..];
                _start = _value.Length;
                return true;
            }
            Current = _value.Slice(_start + 1, end - _start - 1);
            _start = end;
            return true;
        }
    }
}
