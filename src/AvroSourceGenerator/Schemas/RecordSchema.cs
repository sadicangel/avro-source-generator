using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed record class RecordSchema(
    JsonElement Json,
    string Name,
    string? Namespace,
    string? Documentation,
    ImmutableArray<string> Aliases,
    ImmutableArray<Field> Fields)
    : NamedSchema(SchemaType.Record, Json, Name, Namespace, Documentation, Aliases);
