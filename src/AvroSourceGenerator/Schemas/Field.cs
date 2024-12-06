using System.Collections.Immutable;

namespace AvroSourceGenerator.Schemas;

internal sealed record class Field(
    string Name,
    QualifiedName Type,
    string? Documentation,
    ImmutableArray<string> Aliases,
    object? Default,
    int? Order);
