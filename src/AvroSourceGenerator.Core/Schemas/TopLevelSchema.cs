using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public abstract record class TopLevelSchema(
    SchemaType SchemaType,
    SchemaName SchemaName,
    CSharpName CSharpName,
    string? Documentation,
    ImmutableSortedDictionary<string, JsonElement> Properties)
    : AvroSchema(SchemaType, SchemaName, CSharpName, Documentation, Properties);
