using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public sealed record class MapSchema(AvroSchema ValueSchema, string? Documentation, ImmutableSortedDictionary<string, JsonElement> Properties)
    : AvroSchema(SchemaType.Map, new SchemaName(AvroTypeNames.Map), GetCSharpName(ValueSchema), Documentation, Properties)
{
    private static CSharpName GetCSharpName(AvroSchema valueSchema) => new CSharpName($"Dictionary<string, {valueSchema}>", "System.Collections.Generic");
}
