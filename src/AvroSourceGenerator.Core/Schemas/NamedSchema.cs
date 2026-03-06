using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public abstract record class NamedSchema(
    SchemaType Type,
    JsonElement Json,
    SchemaName SchemaName,
    string? Documentation,
    ImmutableArray<string> Aliases,
    ImmutableSortedDictionary<string, JsonElement> Properties)
    : TopLevelSchema(Type, Json, SchemaName, Documentation, Properties);
