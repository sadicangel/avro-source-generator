using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed record class AbstractRecordSchema(
    SchemaName SchemaName,
    ImmutableArray<AvroSchema> DerivedSchemas)
    : TopLevelSchema(
        SchemaType.Abstract,
        default,
        SchemaName,
        GetDefaultDocumentation(DerivedSchemas),
        ImmutableSortedDictionary<string, JsonElement>.Empty)
{
    public override void WriteTo(Utf8JsonWriter writer, HashSet<SchemaName> writtenSchemas, string? containingNamespace) { }

    private static string GetDefaultDocumentation(ImmutableArray<AvroSchema> derivedSchemas)
    {
        var docStart = "Represents a union schema that can be one of the following types: \n<list type=\"bullet\">";

        var items = derivedSchemas.Select(schema => $"\n<item><see cref=\"{schema.CSharpName.FullName}\"/></item>\n");
        return docStart + string.Join(string.Empty, items) + "</list>";
    }
}
