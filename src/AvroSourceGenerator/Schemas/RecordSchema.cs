using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed record class RecordSchema(
    JsonElement Json,
    QualifiedName QualifiedName,
    string? Documentation,
    ImmutableArray<string> Aliases,
    ImmutableArray<Field> Fields)
    : AvroSchema(SchemaType.Record, Json, QualifiedName, Documentation, Aliases);
