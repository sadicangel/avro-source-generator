namespace AvroSourceGenerator.Schemas;

internal sealed record class MapSchema(AvroSchema ValueSchema)
    : AvroSchema(SchemaType.Map, $"IDictionary<string, {ValueSchema}>", "System.Collections.Generic");
