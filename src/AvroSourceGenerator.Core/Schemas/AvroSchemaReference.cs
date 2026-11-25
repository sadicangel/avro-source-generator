using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public sealed record class AvroSchemaReference(SchemaName SchemaName, CSharpName CSharpName)
    : AvroSchema(SchemaType.Reference, SchemaName, CSharpName, Documentation: null, Properties: ImmutableSortedDictionary<string, JsonElement>.Empty)
{
    public AvroSchemaReference(SchemaName schemaName) : this(schemaName, CSharpName.FromSchemaName(schemaName)) { }
}
