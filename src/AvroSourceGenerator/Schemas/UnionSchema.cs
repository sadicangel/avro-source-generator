using System.Collections.Immutable;

namespace AvroSourceGenerator.Schemas;

internal sealed record class UnionSchema(
    string Name,
    string? Namespace,
    ImmutableArray<AvroSchema> Schemas,
    AvroSchema UnderlyingSchema,
    bool IsNullable) : AvroSchema(SchemaType.Union, Name, Namespace);
