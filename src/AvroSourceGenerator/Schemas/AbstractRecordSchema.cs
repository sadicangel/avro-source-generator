using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed record class AbstractRecordSchema(
    SchemaName SchemaName,
    ImmutableArray<AvroSchema> DerivedSchemas)
    : TopLevelSchema(SchemaType.Abstract, default, SchemaName, null, ImmutableSortedDictionary<string, JsonElement>.Empty)
{
    public override void WriteTo(Utf8JsonWriter writer, HashSet<SchemaName> writtenSchemas, string? containingNamespace) { }
}
