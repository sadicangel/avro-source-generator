using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Exceptions;

public sealed class DuplicateSchemaException(AvroSchema schema) : Exception($"Redeclaration of schema '{schema.SchemaName}'")
{
    public AvroSchema Schema { get; } = schema;
}
