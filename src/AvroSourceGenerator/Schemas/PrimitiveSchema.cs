namespace AvroSourceGenerator.Schemas;

internal sealed record class PrimitiveSchema(SchemaType Type, string Name, string? Namespace)
    : AvroSchema(Type, Name, Namespace);
