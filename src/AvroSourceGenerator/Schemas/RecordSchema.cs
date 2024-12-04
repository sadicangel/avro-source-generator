using System.Collections.Immutable;

namespace AvroSourceGenerator.Schemas;

internal sealed record class RecordSchema(
    QualifiedName QualifiedName,
    string? Documentation,
    ImmutableArray<string> Aliases,
    ImmutableArray<Field> Fields)
    : AvroSchema(SchemaType.Record, QualifiedName, Documentation, Aliases);
