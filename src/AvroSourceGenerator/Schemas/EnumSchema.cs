using System.Collections.Immutable;

namespace AvroSourceGenerator.Schemas;

internal sealed record class EnumSchema(
    QualifiedName QualifiedName,
    string? Documentation,
    ImmutableArray<string> Aliases,
    ImmutableArray<string> Symbols,
    string? Default)
    : AvroSchema(SchemaType.Enum, QualifiedName, Documentation, Aliases);
