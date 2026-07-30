using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public abstract record class TopLevelSchema(
    SchemaType Type,
    SchemaName SchemaName,
    string? Documentation,
    ImmutableSortedDictionary<string, JsonElement> Properties)
    : AvroSchema(Type, SchemaName, CSharpName.FromSchemaName(SchemaName), Documentation, Properties);
