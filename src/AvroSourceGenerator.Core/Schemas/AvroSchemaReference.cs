using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public sealed record class AvroSchemaReference(SchemaName SchemaName)
    : AvroSchema(SchemaType.Reference, CSharpName.FromSchemaName(SchemaName), SchemaName, Documentation: null, ImmutableSortedDictionary<string, JsonElement>.Empty)
{
    public override void WriteTo(Utf8JsonWriter writer, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, HashSet<SchemaName> writtenSchemas, string? containingNamespace)
    {
        if (writtenSchemas.Contains(SchemaName))
        {
            writer.WriteStringValue(SchemaName.Namespace == containingNamespace ? SchemaName.Name : SchemaName.FullName);
            return;
        }

        registeredSchemas[SchemaName].WriteTo(writer, registeredSchemas, writtenSchemas, containingNamespace);
    }
}
