using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed record class ErrorSchema(
    JsonElement Json,
    string Name,
    string? Namespace,
    string? Documentation,
    ImmutableArray<string> Aliases,
    ImmutableArray<Field> Fields)
    : NamedSchema(SchemaType.Error, Json, Name, Namespace, Documentation, Aliases);
