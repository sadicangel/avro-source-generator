using System.Collections.Immutable;

namespace AvroSourceGenerator.Schemas;

internal sealed record class FixedSchema(
    QualifiedName QualifiedName,
    string? Documentation,
    ImmutableArray<string> Aliases,
    int Size)
    : AvroSchema(SchemaType.Fixed, QualifiedName, Documentation, Aliases);
