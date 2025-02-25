using System.Collections.Immutable;

namespace AvroSourceGenerator.Schemas;

internal sealed record class Field(
    string Name,
    AvroSchema Type,
    AvroSchema UnderlyingType,
    bool IsNullable,
    string? Documentation,
    ImmutableArray<string> Aliases,
    object? Default,
    int? Order);
