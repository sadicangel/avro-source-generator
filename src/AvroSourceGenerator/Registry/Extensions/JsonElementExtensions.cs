using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry.Extensions;

internal static class JsonElementExtensions
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

    public static string GetValidName(this string name) =>
        IsReserved(name.AsSpan(), out var replacement) ? replacement : name;

    public static ReadOnlySpan<char> GetValidName(ReadOnlySpan<char> name) =>
        IsReserved(name, out var replacement) ? replacement.AsSpan() : name;

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
            if (first) first = false;
            else builder.Append('.');
            builder.EnsureCapacity(builder.Length + name.Length);
            foreach (var @char in name)
                builder.Append(@char);
        }

        return builder.ToString();
    }

    public static SchemaName GetRequiredSchemaName(this string name, string? containingNamespace = null)
    {
        _ = SplitFullName(name, out name, out var @namespace);

        if (string.IsNullOrWhiteSpace(name) || @namespace is "")
            throw new InvalidSchemaException("Argument has an invalid name format: 'cannot start or end with a dot'");

        return new SchemaName(name, @namespace ?? containingNamespace);
    }

    private static bool SplitFullName(string fullName, out string name, out string? @namespace)
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

    extension(JsonElement schema)
    {
        public JsonElement GetRequiredProperty(string propertyName)
        {
            if (!schema.TryGetProperty(propertyName, out var json))
                throw new InvalidSchemaException($"'{propertyName}' property is required in schema: {schema.GetRawText()}");

            return json;
        }

        public JsonElement? GetNullableProperty(string propertyName) =>
            schema.TryGetProperty(propertyName, out var json) ? json : null;

        public JsonElement? GetOptionalProperty(string propertyName) =>
            schema.ValueKind is JsonValueKind.Object && schema.TryGetProperty(propertyName, out var json) ? json : null;

        public string? GetNullableString()
        {
            return schema.ValueKind is JsonValueKind.String && schema.GetString() is { Length: > 0 } value
                ? value
                : null;
        }

        public int? GetOptionalInt32()
        {
            return schema.ValueKind is JsonValueKind.Number && schema.TryGetInt32(out var value)
                ? value
                : null;
        }

        public string GetRequiredString(string propertyName) =>
            schema.GetRequiredProperty(propertyName).GetNullableString()
            ?? throw new InvalidSchemaException($"'{propertyName}' property must be a non-empty, non-whitespace string (found '{schema.GetRequiredProperty(propertyName)}') in schema: {schema.GetRawText()}");

        public string? GetNullableString(string propertyName)
        {
            var maybeJson = schema.GetNullableProperty(propertyName);
            if (maybeJson is null or { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined }) return null;

            var json = maybeJson.Value;
            if (json.ValueKind is not JsonValueKind.String)
                throw new InvalidSchemaException($"'{propertyName}' property must be a string (found '{json}') in schema: {schema.GetRawText()}");

            var value = json.GetString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        public string? GetOptionalString(string propertyName)
        {
            var maybeJson = schema.GetOptionalProperty(propertyName);
            if (maybeJson is null or { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined }) return null;

            var json = maybeJson.Value;
            if (json.ValueKind is not JsonValueKind.String) return null;

            var value = json.GetString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        public int GetRequiredInt32(string propertyName) =>
            schema.GetRequiredProperty(propertyName).GetOptionalInt32()
            ?? throw new InvalidSchemaException($"'{propertyName}' property must be an integer (found '{schema.GetRequiredProperty(propertyName)}') in schema: {schema.GetRawText()}");

        public int? GetNullableInt32(string propertyName)
        {
            var maybeJson = schema.GetNullableProperty(propertyName);
            if (maybeJson is null or { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined }) return null;

            var json = maybeJson.Value;
            if (json.ValueKind is JsonValueKind.Null) return null;

            if (json.ValueKind is not JsonValueKind.Number || !json.TryGetInt32(out var value))
                throw new InvalidSchemaException($"'{propertyName}' property must be an integer (found '{schema.GetRequiredProperty(propertyName)}') in schema: {schema.GetRawText()}");

            return value;
        }

        public JsonElement.ArrayEnumerator GetRequiredArray(string propertyName)
        {
            var json = schema.GetRequiredProperty(propertyName);
            if (json.ValueKind is not JsonValueKind.Array)
                throw new InvalidSchemaException($"'{propertyName}' property must be an array (found '{json}') in schema: {schema.GetRawText()}");
            return json.EnumerateArray();
        }

        public JsonElement.ArrayEnumerator? GetNullableArray(string propertyName)
        {
            var maybeJson = schema.GetNullableProperty(propertyName);
            if (maybeJson is null or { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined }) return null;

            var json = maybeJson.Value;
            if (json.ValueKind is not JsonValueKind.Array)
                throw new InvalidSchemaException($"'{propertyName}' property must be an array (found '{json}') in schema: {schema.GetRawText()}");

            return json.EnumerateArray();
        }

        public JsonElement.ObjectEnumerator GetRequiredObject(string propertyName)
        {
            var json = schema.GetRequiredProperty(propertyName);
            if (json.ValueKind is not JsonValueKind.Object)
                throw new InvalidSchemaException($"'{propertyName}' property must be an object (found '{json}') in schema: {schema.GetRawText()}");

            return json.EnumerateObject();
        }

        public SchemaName GetRequiredSchemaName(
            string? containingNamespace = null,
            string propertyName = "name")
        {
            var name = schema.GetRequiredString(propertyName);

            if (name.IndexOf("..", StringComparison.Ordinal) >= 0)
                throw new InvalidSchemaException($"Property 'name' has an invalid format: 'consecutive dots are not allowed in names or namespaces' in schema: {schema.GetRawText()}");

            // Only use 'namespace' if 'name' isn't a full name.
            if (!SplitFullName(name, out name, out var @namespace))
                @namespace = schema.GetNullableString("namespace");

            if (string.IsNullOrWhiteSpace(name) || @namespace is "")
                throw new InvalidSchemaException($"Property 'name' has an invalid format: 'cannot start or end with a dot' in schema: {schema.GetRawText()}");

            return new SchemaName(name, @namespace ?? containingNamespace);
        }

        public SchemaName GetOptionalSchemaName()
        {
            var name = schema.GetOptionalString("name") ?? schema.GetOptionalString("protocol");

            // 'name' was null (and it was allowed), so we return default.
            if (name is null)
            {
                return default;
            }

            if (name.IndexOf("..", StringComparison.Ordinal) >= 0)
                throw new InvalidSchemaException($"Property 'name' has an invalid format: 'consecutive dots are not allowed in names or namespaces' in schema: {schema.GetRawText()}");

            // Only use 'namespace' if 'name' isn't a full name.
            if (!SplitFullName(name, out name, out var @namespace))
                @namespace = schema.GetNullableString("namespace");

            if (string.IsNullOrWhiteSpace(name) || @namespace is "")
                throw new InvalidSchemaException($"Property 'name' has an invalid format: 'cannot start or end with a dot' in schema: {schema.GetRawText()}");

            return new SchemaName(name, @namespace);
        }

        public string GetSchemaType() =>
            schema.GetRequiredString("type");

        public string? GetDocumentation() =>
            schema.GetNullableString("doc");

        public ImmutableArray<string> GetAliases()
        {
            return schema
                .GetNullableArray("aliases")?
                .Select(json => json.GetNullableString() ?? throw new InvalidSchemaException($"'aliases' property must be an array of non-empty, non-whitespace strings in schema: {schema.GetRawText()}"))
                .ToImmutableArray() ?? [];
        }

        public ImmutableArray<string> GetSymbols()
        {
            return
            [
                .. schema
                    .GetRequiredArray("symbols")
                    .Select(json =>
                        json.GetNullableString()?.GetValidName() ?? throw new InvalidSchemaException($"'symbols' property must be an array of non-empty, non-whitespace strings in schema: {schema.GetRawText()}"))
            ];
        }

        public int GetFixedSize()
        {
            var json = schema.GetRequiredProperty("size");

            return json.ValueKind is JsonValueKind.Number && json.TryGetInt32(out var size) && size > 0
                ? size
                : throw new InvalidSchemaException($"'size' property must be a positive integer (found '{json}') in schema: {schema.GetRawText()}");
        }
    }
}
