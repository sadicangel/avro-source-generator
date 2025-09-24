using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Registry.Extensions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

internal readonly partial struct SchemaRegistry
{
    private AvroSchema Logical(JsonElement schema, string? containingNamespace, ImmutableSortedDictionary<string, JsonElement>? properties = null)
    {
        var logicalType = schema.GetRequiredString("logicalType");

        var underlyingType = schema.GetRequiredString("type");
        var underlyingSchema = underlyingType switch
        {
            "null" => AvroSchema.Object,
            "boolean" => AvroSchema.Boolean,
            "int" => AvroSchema.Int,
            "long" => AvroSchema.Long,
            "float" => AvroSchema.Float,
            "double" => AvroSchema.Double,
            "bytes" => AvroSchema.Bytes,
            "string" => AvroSchema.String,
            "array" => Array(schema, containingNamespace),
            "map" => Map(schema, containingNamespace),
            "enum" => Enum(schema, containingNamespace),
            "record" => Record(schema, containingNamespace),
            "error" => Error(schema, containingNamespace),
            "fixed" => Fixed(schema, containingNamespace),
            _ when _schemas.TryGetValue(
                new SchemaName(JsonElementExtensions.GetValidName(underlyingType),
                containingNamespace), out var topLevelSchema)
            && topLevelSchema is NamedSchema namedSchema => namedSchema,
            _ => throw new InvalidSchemaException($"Unknown schema type '{underlyingType}' in {schema.GetRawText()}")
        };

        properties ??= ImmutableSortedDictionary<string, JsonElement>.Empty;

        return logicalType switch
        {
            "date" => new LogicalSchema(underlyingSchema, new CSharpName("DateTime", "System"), new SchemaName(logicalType), properties),
            "decimal" => new LogicalSchema(underlyingSchema, new CSharpName("AvroDecimal", "Avro"), new SchemaName(logicalType), properties),
            "duration" => new LogicalSchema(underlyingSchema, underlyingSchema.CSharpName, new SchemaName(logicalType), properties),
            "time-micros" => new LogicalSchema(underlyingSchema, new CSharpName("TimeSpan", "System"), new SchemaName(logicalType), properties),
            "time-millis" => new LogicalSchema(underlyingSchema, new CSharpName("TimeSpan", "System"), new SchemaName(logicalType), properties),
            "timestamp-micros" => new LogicalSchema(underlyingSchema, new CSharpName("DateTime", "System"), new SchemaName(logicalType), properties),
            "timestamp-millis" => new LogicalSchema(underlyingSchema, new CSharpName("DateTime", "System"), new SchemaName(logicalType), properties),
            "local-timestamp-micros" => new LogicalSchema(underlyingSchema, new CSharpName("DateTime", "System"), new SchemaName(logicalType), properties),
            "local-timestamp-millis" => new LogicalSchema(underlyingSchema, new CSharpName("DateTime", "System"), new SchemaName(logicalType), properties),
            "uuid" => new LogicalSchema(underlyingSchema, new CSharpName("Guid", "System"), new SchemaName(logicalType), properties),
            // TODO: We should report a warning for unsupported logical types, maybe? But always return the underlying schema.
            _ => underlyingSchema,
        };
    }
}
