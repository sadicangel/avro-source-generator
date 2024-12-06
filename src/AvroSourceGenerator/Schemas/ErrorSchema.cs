using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed record class ErrorSchema(
    JsonElement Json,
    QualifiedName QualifiedName,
    string? Documentation,
    ImmutableArray<string> Aliases,
    ImmutableArray<Field> Fields)
    : AvroSchema(SchemaType.Error, Json, QualifiedName, Documentation, Aliases);
