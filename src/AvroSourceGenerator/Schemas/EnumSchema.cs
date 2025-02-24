using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed record class EnumSchema(
    JsonElement Json,
    string Name,
    string? Namespace,
    string? Documentation,
    ImmutableArray<string> Aliases,
    ImmutableArray<string> Symbols,
    string? Default)
    : NamedSchema(SchemaType.Enum, Json, Name, Namespace, Documentation, Aliases);
