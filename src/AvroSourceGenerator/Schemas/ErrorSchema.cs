using System.Collections.Immutable;

namespace AvroSourceGenerator.Schemas;

internal sealed record class ErrorSchema(
    QualifiedName QualifiedName,
    string? Documentation,
    ImmutableArray<string> Aliases,
    ImmutableArray<Field> Fields)
    : AvroSchema(SchemaType.Error, QualifiedName, Documentation, Aliases);
