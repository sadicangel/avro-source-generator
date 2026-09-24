using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public abstract record class TopLevelSchema(
    SchemaType Type,
    SchemaName SchemaName,
    CSharpName CSharpName,
    string? Documentation,
    ImmutableSortedDictionary<string, JsonElement> Properties)
    : AvroSchema(Type, SchemaName, CSharpName, Documentation, Properties);
