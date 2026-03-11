using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Exceptions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Extensions;

public static class JsonElementAvroExtensions
{
    extension(JsonElement schema)
    {
        public SchemaName GetRequiredSchemaName(string? containingNamespace) =>
            schema.GetRequiredAvroName(
                schema.GetRequiredString(AvroJsonKeys.Name),
                AvroJsonKeys.Name,
                containingNamespace);

        public SchemaName GetRequiredProtocolName(string? containingNamespace) =>
            schema.GetRequiredAvroName(
                schema.GetRequiredString(AvroJsonKeys.Protocol),
                AvroJsonKeys.Protocol,
                containingNamespace);

        private SchemaName GetRequiredAvroName(string property, string propertyName, string? containingNamespace = null)
        {
            if (string.IsNullOrWhiteSpace(property))
                throw new InvalidSchemaException($"'{propertyName}' property must be a non-empty, non-whitespace string (found '{schema.GetNullableProperty(propertyName)}') in schema: {schema.GetRawText()}");

            if (property.IndexOf("..", StringComparison.Ordinal) >= 0)
                throw new InvalidSchemaException($"Property '{propertyName}' has an invalid format: 'consecutive dots are not allowed in names or namespaces' in schema: {schema.GetRawText()}");

            var name = property;
            if (!name.TrySplitQualifiedName(out name, out var @namespace))
                @namespace = schema.GetNullableString(AvroJsonKeys.Namespace);

            if (string.IsNullOrWhiteSpace(name) || @namespace is "")
                throw new InvalidSchemaException($"Property '{propertyName}' has an invalid format: 'cannot start or end with a dot' in schema: {schema.GetRawText()}");

            return new SchemaName(name, @namespace ?? containingNamespace);
        }

        public SchemaName GetOptionalAvroName()
        {
            ReadOnlySpan<string> propertyNames = [AvroJsonKeys.Name, AvroJsonKeys.Protocol];
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
            schema.GetRequiredString(AvroJsonKeys.Type);

        public string? GetLogicalType() =>
            schema.GetOptionalProperty(AvroJsonKeys.LogicalType)?.ToRequiredString();

        public string? GetDocumentation() =>
            schema.GetNullableString(AvroJsonKeys.Doc);

        public ImmutableArray<string> GetAliases()
        {
            return schema
                .GetNullableArray(AvroJsonKeys.Aliases)?
                .Select(json => json.ToOptionalString() ?? throw new InvalidSchemaException($"'{AvroJsonKeys.Aliases}' property must be an array of non-empty, non-whitespace strings in schema: {schema.GetRawText()}"))
                .ToImmutableArray() ?? [];
        }

        public ImmutableArray<string> GetSymbols()
        {
            return
            [
                .. schema
                    .GetRequiredArray(AvroJsonKeys.Symbols)
                    .Select(json =>
                        json.ToOptionalString()?.ToValidName() ?? throw new InvalidSchemaException($"'{AvroJsonKeys.Symbols}' property must be an array of non-empty, non-whitespace strings in schema: {schema.GetRawText()}"))
            ];
        }

        public int GetFixedSize()
        {
            var json = schema.GetRequiredProperty(AvroJsonKeys.Size);

            return json.ValueKind is JsonValueKind.Number && json.TryGetInt32(out var size) && size > 0
                ? size
                : throw new InvalidSchemaException($"'{AvroJsonKeys.Size}' property must be a positive integer (found '{json}') in schema: {schema.GetRawText()}");
        }


        public ImmutableSortedDictionary<string, JsonElement> GetSchemaProperties()
        {
            var properties = ImmutableSortedDictionary.CreateBuilder<string, JsonElement>();
            foreach (var property in schema.EnumerateObject()
                .Where(property => !ReservedSchemaProperties.IsReserved(property.Name)))
            {
                properties.Add(property.Name, property.Value);
            }

            return properties.ToImmutable();
        }

        public ImmutableSortedDictionary<string, JsonElement> GetProtocolProperties()
        {
            var properties = ImmutableSortedDictionary.CreateBuilder<string, JsonElement>();
            foreach (var property in schema.EnumerateObject()
                .Where(property => !ReservedProtocolProperties.IsReserved(property.Name)))
            {
                properties.Add(property.Name, property.Value);
            }

            return properties.ToImmutable();
        }
    }
}
