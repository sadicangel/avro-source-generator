using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal abstract record class NamedSchema(
    SchemaType Type,
    JsonElement Json,
    string Name,
    string? Namespace,
    string? Documentation,
    ImmutableArray<string> Aliases)
    : AvroSchema(Type, Name, Namespace);
