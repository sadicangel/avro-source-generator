using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public sealed record class ArraySchema(AvroSchema ItemSchema, string? Documentation, ImmutableSortedDictionary<string, JsonElement> Properties)
    : AvroSchema(SchemaType.Array, new SchemaName(AvroTypeNames.Array), GetCSharpName(ItemSchema), Documentation, Properties)
{
    private static CSharpName GetCSharpName(AvroSchema itemSchema) => new CSharpName($"List<{itemSchema}>", "System.Collections.Generic");
}
