using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public sealed record class FixedSchema(
    JsonElement Json,
    SchemaName SchemaName,
    string? Documentation,
    ImmutableArray<string> Aliases,
    int Size,
    ImmutableSortedDictionary<string, JsonElement> Properties)
    : NamedSchema(SchemaType.Fixed, Json, SchemaName, Documentation, Aliases, Properties)
{
    public static FixedSchema CreateAsByteArray(
        JsonElement json,
        SchemaName schemaName,
        string? documentation,
        ImmutableArray<string> aliases,
        int size,
        ImmutableSortedDictionary<string, JsonElement> properties)
    {
        return new FixedSchema(json, schemaName, documentation, aliases, size, properties)
        {
            CSharpName = Bytes.CSharpName,
        };
    }

    public override bool ShouldEmitCode => CSharpName != Bytes.CSharpName;

    public override void WriteTo(Utf8JsonWriter writer, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, HashSet<SchemaName> writtenSchemas, string? containingNamespace)
    {
        if (!writtenSchemas.Add(SchemaName))
        {
            var name = SchemaName.Namespace is null || SchemaName.Namespace == containingNamespace ? SchemaName.Name : $"{SchemaName.Namespace}.{SchemaName.Name}";
            writer.WriteStringValue(name);
            return;
        }

        writer.WriteStartObject();
        writer.WriteString(AvroJsonKeys.Type, AvroTypeNames.Fixed);
        writer.WriteString(AvroJsonKeys.Name, SchemaName.Name);
        var @namespace = SchemaName.Namespace ?? containingNamespace;
        if (@namespace is not null)
            writer.WriteString(AvroJsonKeys.Namespace, @namespace);
        if (Documentation is not null)
            writer.WriteString(AvroJsonKeys.Doc, Documentation);
        if (Aliases.Length > 0)
        {
            writer.WriteStartArray(AvroJsonKeys.Aliases);
            foreach (var alias in Aliases)
                writer.WriteStringValue(alias);
            writer.WriteEndArray();
        }

        writer.WriteNumber(AvroJsonKeys.Size, Size);
        foreach (var entry in Properties)
        {
            writer.WritePropertyName(entry.Key);
            entry.Value.WriteTo(writer);
        }

        writer.WriteEndObject();
    }
}
