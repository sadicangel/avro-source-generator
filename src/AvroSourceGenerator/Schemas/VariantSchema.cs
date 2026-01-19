using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal sealed record class VariantSchema(
    SchemaName SchemaName,
    ImmutableArray<AvroSchema> DerivedSchemas)
    : TopLevelSchema(
        SchemaType.Variant,
        default,
        SchemaName,
        GetDefaultDocumentation(DerivedSchemas),
        ImmutableSortedDictionary<string, JsonElement>.Empty)
{
    public VariantSchema(string fieldName, SchemaName containingSchemaName, ImmutableArray<AvroSchema> derivedSchemas)
        : this(GetVariantName(containingSchemaName, fieldName), derivedSchemas) { }

    private static SchemaName GetVariantName(SchemaName containingSchemaName, string fieldName)
    {
        char[] name = ['I', .. containingSchemaName.Name.AsSpan(), .. fieldName.AsSpan(), 'V', 'a', 'r', 'i', 'a', 'n', 't'];
        name[containingSchemaName.Name.Length + 1] = char.ToUpperInvariant(fieldName[0]);

        return new SchemaName(new string(name), containingSchemaName.Namespace);
    }

    public override void WriteTo(Utf8JsonWriter writer, HashSet<SchemaName> writtenSchemas, string? containingNamespace) { }

    private static string GetDefaultDocumentation(ImmutableArray<AvroSchema> derivedSchemas)
    {
        const string NewLine = """


            """;
        var codeReferences = derivedSchemas
            .Where(x => x.Type is not SchemaType.Null)
            .OrderBy(x => x.CSharpName.FullName)
            .Select(x => $"<item><see cref=\"{x.CSharpName.FullName}\"/></item>");

        if (derivedSchemas.Any(x => x.Type is SchemaType.Null))
        {
            codeReferences = codeReferences
                .Prepend("<item><see langword=\"null\"/></item>");
        }

        var documentation = string.Join(
            NewLine,
            [
                "Represents a union schema that can be one of the following:",
                "<list type=\"bullet\">",
                .. codeReferences,
                "</list>"
            ]);

        return documentation;
    }
}
