using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed record class FixedSchema(
    JsonElement Json,
    QualifiedName QualifiedName,
    string? Documentation,
    ImmutableArray<string> Aliases,
    int Size)
    : AvroSchema(SchemaType.Fixed, Json, QualifiedName, Documentation, Aliases);
