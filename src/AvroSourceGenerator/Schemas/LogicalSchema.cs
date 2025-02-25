namespace AvroSourceGenerator.Schemas;

internal sealed record class LogicalSchema(string Name, string? Namespace)
    : AvroSchema(SchemaType.Logical, Name, Namespace);
