using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public sealed record class AvroSchemaReference(SchemaName SchemaName, CSharpName CSharpName)
    : AvroSchema(SchemaType.Reference, SchemaName, CSharpName, Documentation: null, Properties: ImmutableSortedDictionary<string, JsonElement>.Empty)
{
    public AvroSchemaReference(SchemaName SchemaName) : this(SchemaName, CSharpName.FromSchemaName(SchemaName)) { }

    public override void WriteTo(Utf8JsonWriter writer, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, HashSet<SchemaName> writtenSchemas, string? containingNamespace)
    {
        if (writtenSchemas.Contains(SchemaName))
        {
            writer.WriteStringValue(SchemaName.RelativeTo(containingNamespace));
            return;
        }

        registeredSchemas[SchemaName].WriteTo(writer, registeredSchemas, writtenSchemas, containingNamespace);
    }
}
