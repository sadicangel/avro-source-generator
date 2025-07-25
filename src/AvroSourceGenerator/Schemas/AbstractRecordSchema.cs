using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.CodeAnalysis;

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

    /// <summary>
    /// <list type="bullet">
    /// <listheader><term>Represents a union schema that can be one of the following:</term></listheader>
    /// <item><term><see langword="null"/></term></item>
    /// <item><term><see cref="global::com.example.user.EmailContact"/></term></item>
    /// <item><term><see cref="global::com.example.user.PhoneContact"/></term></item>
    /// </list>
    /// </summary>
    private static string GetDefaultDocumentation(ImmutableArray<AvroSchema> derivedSchemas)
    {
        const string NewLine = @"
";
        var codeReferences = derivedSchemas
            .Where(x => x.Type is not SchemaType.Null)
            .OrderBy(x => x.CSharpName.FullName)
            .Select(x => $"<item><see cref=\"{x.CSharpName.FullName}\"/></item>");

        if (derivedSchemas.Any(x => x.Type is SchemaType.Null))
        {
            codeReferences = codeReferences
                .Prepend("<item><see langword=\"null\"/></item>");
        }

        var documentation = string.Join(NewLine, [
            "Represents a union schema that can be one of the following:",
            "<list type=\"bullet\">",
            .. codeReferences,
            "</list>"
            ]);

        return documentation;
    }
}
