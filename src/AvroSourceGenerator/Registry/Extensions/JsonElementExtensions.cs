using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry.Extensions;

internal static class JsonElementExtensions
{
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
            if (!name.TrySplitQualifiedName(out name, out var @namespace))
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
            if (!name.TrySplitQualifiedName(out name, out var @namespace))
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
                        json.GetNullableString()?.ToValidName() ?? throw new InvalidSchemaException($"'symbols' property must be an array of non-empty, non-whitespace strings in schema: {schema.GetRawText()}"))
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
