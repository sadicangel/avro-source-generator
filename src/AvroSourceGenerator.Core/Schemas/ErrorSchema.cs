using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public sealed record class ErrorSchema(
    SchemaName SchemaName,
    string? Documentation,
    ImmutableArray<string> Aliases,
    ImmutableArray<Field> Fields,
    ImmutableSortedDictionary<string, JsonElement> Properties)
    : NamedSchema(SchemaType.Error, SchemaName, Documentation, Aliases, Properties);
