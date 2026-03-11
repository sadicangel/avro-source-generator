using System.Text.Json;
using AvroSourceGenerator.Exceptions;

namespace AvroSourceGenerator.Extensions;

internal static class JsonElementJsonExtensions
{
    extension(JsonElement schema)
    {
        public JsonElement GetRequiredProperty(string propertyName) => !schema.TryGetProperty(propertyName, out var json)
            ? throw new InvalidSchemaException($"'{propertyName}' property is required in schema: {schema.GetRawText()}")
            : json;

        public JsonElement? GetNullableProperty(string propertyName) =>
            schema.TryGetProperty(propertyName, out var json) ? json : null;

        public JsonElement? GetOptionalProperty(string propertyName) =>
            schema.ValueKind is JsonValueKind.Object && schema.TryGetProperty(propertyName, out var json) ? json : null;

        public string ToRequiredString() =>
            schema is { ValueKind: JsonValueKind.String } && schema.GetString() is { Length: > 0 } value ? value : throw new InvalidSchemaException($"Expected a non-empty, non-whitespace string (found '{schema}') in schema: {schema.GetRawText()}");

        public string? ToOptionalString() =>
            schema is { ValueKind: JsonValueKind.String } && schema.GetString() is { Length: > 0 } value ? value : null;

        public string GetRequiredString(string propertyName) =>
            schema.GetRequiredProperty(propertyName).ToOptionalString()
            ?? throw new InvalidSchemaException($"'{propertyName}' property must be a non-empty, non-whitespace string (found '{schema.GetNullableProperty(propertyName)}') in schema: {schema.GetRawText()}");

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

        public string? GetOptionalString(string propertyName) =>
            schema.GetOptionalProperty(propertyName)?.ToOptionalString();

        public int? GetNullableInt32(string propertyName)
        {
            var maybeJson = schema.GetNullableProperty(propertyName);
            if (maybeJson is null or { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined }) return null;

            var json = maybeJson.Value;
            if (json.ValueKind is JsonValueKind.Null) return null;

            if (json.ValueKind is not JsonValueKind.Number || !json.TryGetInt32(out var value))
                throw new InvalidSchemaException($"'{propertyName}' property must be an integer (found '{schema.GetNullableProperty(propertyName)}') in schema: {schema.GetRawText()}");

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
    }
}
