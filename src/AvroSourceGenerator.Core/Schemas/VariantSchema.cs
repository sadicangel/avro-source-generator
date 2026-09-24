using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public sealed record class VariantSchema(
    SchemaName SchemaName,
    CSharpName CSharpName,
    ImmutableArray<AvroSchema> DerivedSchemas)
    : TopLevelSchema(
        SchemaType.Variant,
        SchemaName,
        CSharpName,
        GetDefaultDocumentation(DerivedSchemas),
        ImmutableSortedDictionary<string, JsonElement>.Empty)
{
    internal static SchemaName GetSchemaName(SchemaName containingSchemaName, FieldName fieldName)
    {
        var length = 1 + containingSchemaName.Name.Length + fieldName.SchemaName.Length + 7;
        var name = string.Create(
            length,
            (fieldName.SchemaName, containingSchemaName.Name),
            static (span, state) =>
            {
                var (fieldName, containingSchemaName) = state;
                span[0] = 'I';
                span = span[1..];
                containingSchemaName.AsSpan().CopyTo(span);
                span = span[containingSchemaName.Length..];
                fieldName.AsSpan().CopyTo(span);
                span[0] = char.ToUpperInvariant(span[0]);
                span = span[fieldName.Length..];
                "Variant".AsSpan().CopyTo(span);
            });

        return new SchemaName(name, containingSchemaName.Namespace);
    }

    public override void WriteTo(Utf8JsonWriter writer, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, HashSet<SchemaName> writtenSchemas, string? containingNamespace) { }

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
