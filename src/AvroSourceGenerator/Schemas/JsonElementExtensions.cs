using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal static class JsonElementExtensions
{
    // TODO: Avoid allocating a new string to compare against "null".
    public static bool IsNullSchema(JsonElement schema) =>
        schema.ValueKind is JsonValueKind.String && schema.GetString() == "null";

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

    public static string GetValid(string name) => IsReserved(name.AsSpan(), out var replacement) ? replacement : name;

    public static ReadOnlySpan<char> GetValid(ReadOnlySpan<char> name) => IsReserved(name, out var replacement) ? replacement.AsSpan() : name;

    public static string GetLocalName(this JsonElement schema)
    {
        if (!schema.TryGetProperty("name"u8, out var json))
        {
            throw new InvalidSchemaException($"'name' property is required in schema: {schema.GetRawText()}");
        }

        if (json.ValueKind is not JsonValueKind.String)
        {
            throw new InvalidSchemaException($"'name' property must be a string in schema: {schema.GetRawText()}");
        }

        if (json.GetString() is not string name || string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidSchemaException($"'name' property cannot be whitespace in schema: {schema.GetRawText()}");
        }

        return GetValid(name);
    }

    public static string? GetNamespace(this JsonElement schema)
    {
        if (!schema.TryGetProperty("namespace"u8, out var json) || json.ValueKind is JsonValueKind.Null)
        {
            return null;
        }

        if (json.ValueKind is not JsonValueKind.String)
        {
            throw new InvalidSchemaException($"'namespace' property must be a string in schema: {schema.GetRawText()}");
        }

        if (json.GetString() is not string @namespace || string.IsNullOrWhiteSpace(@namespace))
        {
            throw new InvalidSchemaException($"'namespace' property cannot be whitespace in schema: {schema.GetRawText()}");
        }

        var builder = new StringBuilder("global::");

        var first = true;
        foreach (var part in new SplitEnumerable(@namespace.AsSpan(), '.'))
        {
            var name = GetValid(part);
            if (first) first = false; else builder.Append('.');
            builder.EnsureCapacity(builder.Length + name.Length);
            for (var i = 0; i < name.Length; ++i)
                builder.Append(name[i]);
        }

        return builder.ToString();
    }

    public static JsonElement GetSchemaType(this JsonElement schema)
    {
        if (!schema.TryGetProperty("type"u8, out var json))
        {
            throw new InvalidSchemaException($"'type' property is required in schema: {schema.GetRawText()}");
        }

        return json;
    }

    public static string GetSchemaTypeString(this JsonElement schema)
    {
        var json = schema.GetSchemaType();

        if (json.ValueKind is not JsonValueKind.String)
        {
            throw new InvalidSchemaException($"'type' property must be a string in schema: {schema.GetRawText()}");
        }

        var type = json.GetString();
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new InvalidSchemaException($"'type' property cannot be whitespace in schema: {schema.GetRawText()}");
        }

        return type!;
    }

    public static string? GetDocumentation(this JsonElement schema)
    {
        if (!schema.TryGetProperty("doc"u8, out var doc) || doc.ValueKind is JsonValueKind.Null)
        {
            return null;
        }

        if (doc.ValueKind is not JsonValueKind.String)
        {
            // TODO: Allow ignoring invalid non-required properties.
            throw new InvalidSchemaException($"'doc' property must be a string in schema: {schema.GetRawText()}");
        }

        return doc.GetString();
    }

    public static ImmutableArray<string> GetAliases(this JsonElement schema)
    {
        if (!schema.TryGetProperty("aliases"u8, out var ali) || ali.ValueKind is JsonValueKind.Null)
        {
            return [];
        }

        if (ali.ValueKind is not JsonValueKind.Array)
        {
            // TODO: Allow ignoring invalid non-required properties.
            throw new InvalidSchemaException($"'aliases' property must be an array in schema: {schema.GetRawText()}");
        }

        return ali
            .EnumerateArray()
            .Select(json =>
            {
                // TODO: Allow ignoring invalid non-required properties.
                if (json.ValueKind is not JsonValueKind.String)
                {
                    throw new InvalidSchemaException($"'aliases' property must be an array of strings in schema: {schema.GetRawText()}");
                }

                var alias = json.GetString();
                if (string.IsNullOrWhiteSpace(alias))
                {
                    throw new InvalidSchemaException($"'aliases' property cannot whitespace only aliases in schema: {schema.GetRawText()}");
                }
                return alias!;
            })
            .ToImmutableArray();
    }

    public static JsonElement GetSchemaValues(this JsonElement schema)
    {
        if (!schema.TryGetProperty("values"u8, out var valuesSchema) || valuesSchema.ValueKind is JsonValueKind.Null)
        {
            throw new InvalidSchemaException($"'values' property is required in schema: {schema.GetRawText()}");
        }

        return valuesSchema;
    }

    public static JsonElement GetSchemaItems(this JsonElement schema)
    {
        if (!schema.TryGetProperty("items"u8, out var itemsSchema) || itemsSchema.ValueKind is JsonValueKind.Null)
        {
            throw new InvalidSchemaException($"'items' property is required in schema: {schema.GetRawText()}");
        }
        return itemsSchema;
    }

    public static ImmutableArray<string> GetSymbols(this JsonElement schema)
    {
        if (!schema.TryGetProperty("symbols"u8, out var symbols) || symbols.ValueKind is JsonValueKind.Null)
        {
            throw new InvalidSchemaException($"'symbols' property is required in schema: {schema.GetRawText()}");
        }

        if (symbols.ValueKind is not JsonValueKind.Array)
        {
            throw new InvalidSchemaException($"'symbols' property must be an array in schema: {schema.GetRawText()}");
        }

        return symbols
            .EnumerateArray()
            .Select(json =>
            {
                if (json.ValueKind is not JsonValueKind.String)
                {
                    throw new InvalidSchemaException($"'symbols' property must be an array of strings in schema: {schema.GetRawText()}");
                }

                var symbol = json.GetString();
                if (string.IsNullOrWhiteSpace(symbol))
                {
                    throw new InvalidSchemaException($"'symbols' property cannot contain whitespace only symbols in schema: {schema.GetRawText()}");
                }

                return GetValid(symbol!);
            })
            .ToImmutableArray();
    }

    public static JsonElement.ArrayEnumerator GetSchemaFields(this JsonElement schema)
    {
        if (!schema.TryGetProperty("fields"u8, out var fields) || fields.ValueKind is JsonValueKind.Null)
        {
            throw new InvalidSchemaException($"'fields' property is required in schema: {schema.GetRawText()}");
        }
        if (fields.ValueKind is not JsonValueKind.Array)
        {
            throw new InvalidSchemaException($"'fields' property must be an array in schema: {schema.GetRawText()}");
        }

        return fields.EnumerateArray();
    }

    public static int GetFixedSize(this JsonElement schema)
    {
        if (!schema.TryGetProperty("size"u8, out var json))
        {
            throw new InvalidSchemaException($"'size' property is required in schema: {schema.GetRawText()}");
        }
        if (json.ValueKind is not JsonValueKind.Number)
        {
            throw new InvalidSchemaException($"'size' property must be a number in schema: {schema.GetRawText()}");
        }
        if (!json.TryGetInt32(out var size))
        {
            throw new InvalidSchemaException($"'size' property must be an integer in schema: {schema.GetRawText()}");
        }
        if (size <= 0)
        {
            throw new InvalidSchemaException($"'size' property must be a positive integer in schema: {schema.GetRawText()}");
        }

        return size;
    }

    public static bool IsSupportedLogicalSchema(this JsonElement schema)
    {
        if (!schema.TryGetProperty("logicalType"u8, out var logicalType) || logicalType.ValueKind is not JsonValueKind.String)
        {
            return false;
        }

        return logicalType.ValueEquals("date"u8)
            || logicalType.ValueEquals("decimal"u8)
            // logicalType.ValueEquals("duration"u8) is handled as a fixed type
            || logicalType.ValueEquals("time-millis"u8)
            || logicalType.ValueEquals("time-micros"u8)
            || logicalType.ValueEquals("timestamp-millis"u8)
            || logicalType.ValueEquals("timestamp-micros"u8)
            || logicalType.ValueEquals("local-timestamp-millis"u8)
            || logicalType.ValueEquals("local-timestamp-micros"u8)
            || logicalType.ValueEquals("uuid"u8);
    }

    public static string GetLogicalType(this JsonElement schema)
    {
        if (!schema.TryGetProperty("logicalType"u8, out var logicalType))
        {
            throw new InvalidSchemaException($"'logicalType' property is required in schema: {schema.GetRawText()}");
        }
        if (logicalType.ValueKind is not JsonValueKind.String)
        {
            throw new InvalidSchemaException($"'logicalType' property must be a string in schema: {schema.GetRawText()}");
        }

        return logicalType.GetString()
            ?? throw new InvalidSchemaException($"Invalid logical type in schema: {schema.GetRawText()}");
    }

    public static int? GetFieldOrder(this JsonElement field)
    {
        if (field.TryGetProperty("order"u8, out var json))
        {
            if (json.ValueKind is not JsonValueKind.Number)
            {
                throw new InvalidSchemaException($"'order' property must be a number in field: {field.GetRawText()}");
            }

            if (!json.TryGetInt32(out var order))
            {
                throw new InvalidSchemaException($"'order' property must be an integer in field: {field.GetRawText()}");
            }

            return order;
        }
        return null;
    }

    public static JsonElement GetSchemaDefault(this JsonElement schema)
    {
        if (schema.TryGetProperty("default"u8, out var json))
        {
            return json;
        }
        return default;
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
            Current = _value.Slice(_start + 1, end);
            _start += end + 1;
            return true;
        }
    }
}
