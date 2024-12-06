using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed record class EnumSchema(
    JsonElement Json,
    QualifiedName QualifiedName,
    string? Documentation,
    ImmutableArray<string> Aliases,
    ImmutableArray<string> Symbols,
    string? Default)
    : AvroSchema(SchemaType.Enum, Json, QualifiedName, Documentation, Aliases);
