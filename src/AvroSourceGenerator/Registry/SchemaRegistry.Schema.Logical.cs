using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Registry.Extensions;
using AvroSourceGenerator.Schemas;
using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.Registry;

internal readonly partial struct SchemaRegistry
{
    private AvroSchema Logical(
        JsonElement schema,
        AvroSchema underlyingSchema,
        ImmutableSortedDictionary<string, JsonElement> properties)
    {
        var logicalType = schema.GetRequiredString("logicalType");

        return avroLibrary switch
        {
            AvroLibrary.None when languageVersion >= LanguageVersion.CSharp10 =>
                LogicalNoneCSharp10OrLater.Create(logicalType, underlyingSchema, properties),
            AvroLibrary.None =>
                LogicalNone.Create(logicalType, underlyingSchema, properties),
            AvroLibrary.Apache =>
                LogicalApache.Create(logicalType, underlyingSchema, properties),
            AvroLibrary.Auto =>
                throw new NotSupportedException($"Invalid {nameof(AvroLibrary)} '{avroLibrary}'"),
            _ =>
                throw new NotSupportedException($"Invalid {nameof(AvroLibrary)} '{avroLibrary}'"),
        };
    }
}

file static class LogicalNone
{
    public static AvroSchema Create(
        string logicalType,
        AvroSchema underlyingSchema,
        ImmutableSortedDictionary<string, JsonElement> properties) => logicalType switch
        {
            "date" => new LogicalSchema(
                underlyingSchema,
                new CSharpName("DateTime", "System"),
                new SchemaName(logicalType),
                properties),
            "decimal" => new LogicalSchema(
                underlyingSchema,
                new CSharpName("decimal"),
                new SchemaName(logicalType),
                properties),
            "duration" => new LogicalSchema(
                underlyingSchema,
                underlyingSchema.CSharpName,
                new SchemaName(logicalType),
                properties),
            "time-micros" => new LogicalSchema(
                underlyingSchema,
                new CSharpName("TimeTime", "System"),
                new SchemaName(logicalType),
                properties),
            "time-millis" => new LogicalSchema(
                underlyingSchema,
                new CSharpName("TimeTime", "System"),
                new SchemaName(logicalType),
                properties),
            "timestamp-micros" => new LogicalSchema(
                underlyingSchema,
                new CSharpName("DateTime", "System"),
                new SchemaName(logicalType),
                properties),
            "timestamp-millis" => new LogicalSchema(
                underlyingSchema,
                new CSharpName("DateTime", "System"),
                new SchemaName(logicalType),
                properties),
            "local-timestamp-micros" => new LogicalSchema(
                underlyingSchema,
                new CSharpName("DateTime", "System"),
                new SchemaName(logicalType),
                properties),
            "local-timestamp-millis" => new LogicalSchema(
                underlyingSchema,
                new CSharpName("DateTime", "System"),
                new SchemaName(logicalType),
                properties),
            "uuid" => new LogicalSchema(
                underlyingSchema,
                new CSharpName("Guid", "System"),
                new SchemaName(logicalType),
                properties),
            _ => underlyingSchema,
        };
}

file static class LogicalNoneCSharp10OrLater
{
    public static AvroSchema Create(
        string logicalType,
        AvroSchema underlyingSchema,
        ImmutableSortedDictionary<string, JsonElement> properties) => logicalType switch
        {
            "date" => new LogicalSchema(
                underlyingSchema,
                new CSharpName("DateOnly", "System"),
                new SchemaName(logicalType),
                properties),
            "decimal" => new LogicalSchema(
                underlyingSchema,
                new CSharpName("decimal"),
                new SchemaName(logicalType),
                properties),
            "duration" => new LogicalSchema(
                underlyingSchema,
                underlyingSchema.CSharpName,
                new SchemaName(logicalType),
                properties),
            "time-micros" => new LogicalSchema(
                underlyingSchema,
                new CSharpName("TimeOnly", "System"),
                new SchemaName(logicalType),
                properties),
            "time-millis" => new LogicalSchema(
                underlyingSchema,
                new CSharpName("TimeOnly", "System"),
                new SchemaName(logicalType),
                properties),
            "timestamp-micros" => new LogicalSchema(
                underlyingSchema,
                new CSharpName("DateTime", "System"),
                new SchemaName(logicalType),
                properties),
            "timestamp-millis" => new LogicalSchema(
                underlyingSchema,
                new CSharpName("DateTime", "System"),
                new SchemaName(logicalType),
                properties),
            "local-timestamp-micros" => new LogicalSchema(
                underlyingSchema,
                new CSharpName("DateTime", "System"),
                new SchemaName(logicalType),
                properties),
            "local-timestamp-millis" => new LogicalSchema(
                underlyingSchema,
                new CSharpName("DateTime", "System"),
                new SchemaName(logicalType),
                properties),
            "uuid" => new LogicalSchema(
                underlyingSchema,
                new CSharpName("Guid", "System"),
                new SchemaName(logicalType),
                properties),
            _ => underlyingSchema,
        };
}

file static class LogicalApache
{
    public static AvroSchema Create(
        string logicalType,
        AvroSchema underlyingSchema,
        ImmutableSortedDictionary<string, JsonElement> properties) => logicalType switch
        {
            "date" => new LogicalSchema(
                underlyingSchema,
                new CSharpName("DateTime", "System"),
                new SchemaName(logicalType),
                properties),
            "decimal" => new LogicalSchema(
                underlyingSchema,
                new CSharpName("AvroDecimal", "Avro"),
                new SchemaName(logicalType),
                properties),
            "duration" => new LogicalSchema(
                underlyingSchema,
                underlyingSchema.CSharpName,
                new SchemaName(logicalType),
                properties),
            "time-micros" => new LogicalSchema(
                underlyingSchema,
                new CSharpName("TimeSpan", "System"),
                new SchemaName(logicalType),
                properties),
            "time-millis" => new LogicalSchema(
                underlyingSchema,
                new CSharpName("TimeSpan", "System"),
                new SchemaName(logicalType),
                properties),
            "timestamp-micros" => new LogicalSchema(
                underlyingSchema,
                new CSharpName("DateTime", "System"),
                new SchemaName(logicalType),
                properties),
            "timestamp-millis" => new LogicalSchema(
                underlyingSchema,
                new CSharpName("DateTime", "System"),
                new SchemaName(logicalType),
                properties),
            "local-timestamp-micros" => new LogicalSchema(
                underlyingSchema,
                new CSharpName("DateTime", "System"),
                new SchemaName(logicalType),
                properties),
            "local-timestamp-millis" => new LogicalSchema(
                underlyingSchema,
                new CSharpName("DateTime", "System"),
                new SchemaName(logicalType),
                properties),
            "uuid" => new LogicalSchema(
                underlyingSchema,
                new CSharpName("Guid", "System"),
                new SchemaName(logicalType),
                properties),
            _ => underlyingSchema,
        };
}
