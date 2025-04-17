using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas.Extensions;

internal static class JsonElementExtensions
{
    public static bool IsNullOrEmptyString(this JsonElement json) => json.ValueEquals((string?)null);

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

    public static string GetValidName(this string name) => IsReserved(name.AsSpan(), out var replacement) ? replacement : name;

    public static ReadOnlySpan<char> GetValidName(ReadOnlySpan<char> name) => IsReserved(name, out var replacement) ? replacement.AsSpan() : name;

    public static string GetValidNamespace(this string @namespace) =>
        @namespace.Contains('.') ? GetValidNamespace(@namespace.AsSpan()) : GetValidName(@namespace);

    public static string GetValidNamespace(ReadOnlySpan<char> @namespace)
    {
        // TODO: Might be a good idea to pool this builder.
        var builder = new StringBuilder();

        var first = true;
        foreach (var part in new SplitEnumerable(@namespace, '.'))
        {
            var name = GetValidName(part);
            if (first) first = false; else builder.Append('.');
            builder.EnsureCapacity(builder.Length + name.Length);
            for (var i = 0; i < name.Length; ++i)
                builder.Append(name[i]);
        }

        return builder.ToString();
    }

    public static JsonElement GetRequiredProperty(this JsonElement schema, string propertyName)
    {
        if (!schema.TryGetProperty(propertyName, out var json))
            throw new InvalidSchemaException($"'{propertyName}' property is required in schema: {schema.GetRawText()}");

        return json;
    }
    public static JsonElement? GetNullableProperty(this JsonElement schema, string propertyName) =>
        schema.TryGetProperty(propertyName, out var json) ? json : null;

    public static string? GetNullableString(this JsonElement json)
    {
        return json.ValueKind is JsonValueKind.String && json.GetString() is string { Length: > 0 } value
            ? value
            : null;
    }

    public static int? GetOptionalInt32(this JsonElement json)
    {
        return json.ValueKind is JsonValueKind.Number && json.TryGetInt32(out var value)
            ? value
            : null;
    }

    public static string GetRequiredString(this JsonElement schema, string propertyName) =>
        schema.GetRequiredProperty(propertyName).GetNullableString()
            ?? throw new InvalidSchemaException($"'{propertyName}' property must be a non-empty, non-whitespace string (found '{schema.GetRequiredProperty(propertyName)}') in schema: {schema.GetRawText()}");

    public static string? GetNullableString(this JsonElement schema, string propertyName)
    {
        var maybeJson = schema.GetNullableProperty(propertyName);
        if (maybeJson is null or { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined }) return null;

        var json = maybeJson.Value;
        if (json.ValueKind is not JsonValueKind.String and not JsonValueKind.Null)
            throw new InvalidSchemaException($"'{propertyName}' property must be a string (found '{json}') in schema: {schema.GetRawText()}");

        var value = json.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public static int GetRequiredInt32(this JsonElement schema, string propertyName) =>
        schema.GetRequiredProperty(propertyName).GetOptionalInt32()
            ?? throw new InvalidSchemaException($"'{propertyName}' property must be an integer (found '{schema.GetRequiredProperty(propertyName)}') in schema: {schema.GetRawText()}");

    public static int? GetNullableInt32(this JsonElement schema, string propertyName)
    {
        var maybeJson = schema.GetNullableProperty(propertyName);
        if (maybeJson is null or { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined }) return null;

        var json = maybeJson.Value;
        if (json.ValueKind is JsonValueKind.Null) return null;

        if (json.ValueKind is not JsonValueKind.Number || !json.TryGetInt32(out var value))
            throw new InvalidSchemaException($"'{propertyName}' property must be an integer (found '{schema.GetRequiredProperty(propertyName)}') in schema: {schema.GetRawText()}");

        return value;
    }

    public static JsonElement.ArrayEnumerator GetRequiredArray(this JsonElement schema, string propertyName)
    {
        var json = schema.GetRequiredProperty(propertyName);
        if (json.ValueKind is not JsonValueKind.Array)
            throw new InvalidSchemaException($"'{propertyName}' property must be an array (found '{json}') in schema: {schema.GetRawText()}");
        return json.EnumerateArray();
    }

    public static JsonElement.ArrayEnumerator? GetNullableArray(this JsonElement schema, string propertyName)
    {
        var maybeJson = schema.GetNullableProperty(propertyName);
        if (maybeJson is null or { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined }) return null;

        var json = maybeJson.Value;
        if (json.ValueKind is not JsonValueKind.Array)
            throw new InvalidSchemaException($"'{propertyName}' property must be an array (found '{json}') in schema: {schema.GetRawText()}");

        return json.EnumerateArray();
    }

    public static string GetName(this JsonElement schema, out string? @namespace)
    {
        var name = schema.GetName();

        if (name.IndexOf("..") >= 0)
            throw new InvalidSchemaException(
                $"Property 'name' has an invalid format: 'consecutive dots are not allowed in names or namespaces' in schema: {schema.GetRawText()}");

        // Only use 'namespace' if 'name' isn't a full name.
        if (!SplitFullName(name, out name, out @namespace))
            @namespace = schema.GetNamespace();

        if (string.IsNullOrWhiteSpace(name) || @namespace is "")
            throw new InvalidSchemaException(
                $"Property 'name' has an invalid format: 'cannot start or end with a dot' in schema: {schema.GetRawText()}");

        return name;

        static bool SplitFullName(string fullName, out string name, out string? @namespace)
        {
            var indexOfLast = fullName.LastIndexOf('.');
            if (indexOfLast < 0)
            {
                name = fullName;
                @namespace = null;
                return false;
            }

            name = fullName[(indexOfLast + 1)..];
            @namespace = GetValidNamespace(fullName.AsSpan(0, indexOfLast));

            return true;
        }
    }

    public static string GetName(this JsonElement schema) =>
        schema.GetRequiredString("name").GetValidName();

    public static string? GetNamespace(this JsonElement schema) =>
        schema.GetNullableString("namespace")?.GetValidNamespace();

    public static string GetSchemaType(this JsonElement schema) =>
        schema.GetRequiredString("type");

    public static string? GetDocumentation(this JsonElement schema) =>
        schema.GetNullableString("doc");

    public static ImmutableArray<string> GetAliases(this JsonElement schema)
    {
        return schema
            .GetNullableArray("aliases")?
            .Select(json => json.GetNullableString() ?? throw new InvalidSchemaException($"'aliases' property must be an array of non-empty, non-whitespace strings in schema: {schema.GetRawText()}"))?
            .ToImmutableArray() ?? [];
    }

    public static ImmutableArray<string> GetSymbols(this JsonElement schema)
    {
        return [.. schema
            .GetRequiredArray("symbols")
            .Select(json => json.GetNullableString()?.GetValidName() ?? throw new InvalidSchemaException($"'symbols' property must be an array of non-empty, non-whitespace strings in schema: {schema.GetRawText()}"))];
    }

    public static int GetFixedSize(this JsonElement schema)
    {
        var json = schema.GetRequiredProperty("size");

        return json.ValueKind is JsonValueKind.Number && json.TryGetInt32(out var size) && size > 0
            ? size
            : throw new InvalidSchemaException($"'size' property must be a positive integer (found '{json}') in schema: {schema.GetRawText()}");
    }

    public static bool IsSupportedLogicalSchema(this JsonElement schema)
    {
        if (!schema.TryGetProperty("logicalType", out var logicalType) || logicalType.ValueKind is not JsonValueKind.String)
            return false;

        return logicalType.ValueEquals("date")
            || logicalType.ValueEquals("decimal")
            // logicalType.ValueEquals("duration") is handled as a fixed type
            || logicalType.ValueEquals("time-millis")
            || logicalType.ValueEquals("time-micros")
            || logicalType.ValueEquals("timestamp-millis")
            || logicalType.ValueEquals("timestamp-micros")
            || logicalType.ValueEquals("local-timestamp-millis")
            || logicalType.ValueEquals("local-timestamp-micros")
            || logicalType.ValueEquals("uuid");
    }
}
