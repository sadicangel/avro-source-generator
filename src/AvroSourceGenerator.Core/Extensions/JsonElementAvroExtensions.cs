using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Extensions;

public static class JsonElementAvroExtensions
{
    extension(JsonElement schema)
    {
        public SchemaName GetRequiredSchemaName(string? containingNamespace) =>
            schema.GetRequiredAvroName(schema.GetRequiredString("name"), "name", containingNamespace);

        public SchemaName GetRequiredProtocolName(string? containingNamespace) =>
            schema.GetRequiredAvroName(schema.GetRequiredString("protocol"), "protocol", containingNamespace);

        private SchemaName GetRequiredAvroName(string property, string propertyName, string? containingNamespace = null)
        {
            if (string.IsNullOrWhiteSpace(property))
                throw new InvalidSchemaException($"'{propertyName}' property must be a non-empty, non-whitespace string (found '{schema.GetNullableProperty(propertyName)}') in schema: {schema.GetRawText()}");

            if (property.IndexOf("..", StringComparison.Ordinal) >= 0)
                throw new InvalidSchemaException($"Property '{propertyName}' has an invalid format: 'consecutive dots are not allowed in names or namespaces' in schema: {schema.GetRawText()}");

            var name = property;
            if (!name.TrySplitQualifiedName(out name, out var @namespace))
                @namespace = schema.GetNullableString("namespace");

            if (string.IsNullOrWhiteSpace(name) || @namespace is "")
                throw new InvalidSchemaException($"Property '{propertyName}' has an invalid format: 'cannot start or end with a dot' in schema: {schema.GetRawText()}");

            return new SchemaName(name, @namespace ?? containingNamespace);
        }

        public SchemaName GetOptionalAvroName()
        {
            ReadOnlySpan<string> propertyNames = ["name", "protocol"];
            foreach (var propertyName in propertyNames)
            {
                var property = schema.GetOptionalString(propertyName);
                if (property is null)
                {
                    continue;
                }

                return schema.GetRequiredAvroName(property, propertyName);
            }

            return default;
        }

        public string GetSchemaType() =>
            schema.GetRequiredString("type");

        public string? GetDocumentation() =>
            schema.GetNullableString("doc");

        public ImmutableArray<string> GetAliases()
        {
            return schema
                .GetNullableArray("aliases")?
                .Select(json => json.ToOptionalString() ?? throw new InvalidSchemaException($"'aliases' property must be an array of non-empty, non-whitespace strings in schema: {schema.GetRawText()}"))
                .ToImmutableArray() ?? [];
        }

        public ImmutableArray<string> GetSymbols()
        {
            return
            [
                .. schema
                    .GetRequiredArray("symbols")
                    .Select(json =>
                        json.ToOptionalString()?.ToValidName() ?? throw new InvalidSchemaException($"'symbols' property must be an array of non-empty, non-whitespace strings in schema: {schema.GetRawText()}"))
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
