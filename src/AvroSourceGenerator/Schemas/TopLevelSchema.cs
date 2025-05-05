using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal abstract record class TopLevelSchema(
    SchemaType Type,
    JsonElement Json,
    SchemaName SchemaName,
    string? Documentation,
    ImmutableSortedDictionary<string, JsonElement> Properties)
    : AvroSchema(Type, CSharpName.FromSchemaName(SchemaName), SchemaName);
