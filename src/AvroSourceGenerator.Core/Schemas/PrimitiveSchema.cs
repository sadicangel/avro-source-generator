using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public sealed record class PrimitiveSchema(SchemaType SchemaType, SchemaName SchemaName, CSharpName CSharpName, string? Documentation, ImmutableSortedDictionary<string, JsonElement> Properties)
    : AvroSchema(SchemaType, SchemaName, CSharpName, Documentation, Properties)
{
    public PrimitiveSchema(SchemaType schemaType, CSharpName csharpName, SchemaName schemaName)
        : this(schemaType, schemaName, csharpName, Documentation: null, Properties: ImmutableSortedDictionary<string, JsonElement>.Empty) { }
}
