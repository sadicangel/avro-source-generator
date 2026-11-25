using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Protocols;

public sealed record class ProtocolSchema(
    SchemaName SchemaName,
    string? Documentation,
    ImmutableArray<NamedSchema> Types,
    ImmutableArray<ProtocolMessage> Messages,
    ImmutableSortedDictionary<string, JsonElement> Properties)
    : TopLevelSchema(SchemaType.Protocol, SchemaName, CSharpName.FromSchemaName(SchemaName), Documentation, Properties);
