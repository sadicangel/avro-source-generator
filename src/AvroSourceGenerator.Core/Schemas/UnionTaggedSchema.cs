using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public sealed record class UnionTaggedSchema(SchemaName SchemaName, CSharpName CSharpName, ImmutableArray<AvroSchema> MemberSchemas)
    : TopLevelSchema(
        SchemaType.UnionTagged,
        SchemaName,
        CSharpName,
        UnionSchemaHelpers.GetDocumentation(MemberSchemas),
        ImmutableSortedDictionary<string, JsonElement>.Empty)
{
    internal static SchemaName GetSchemaName(SchemaName typeName, FieldName fieldName) =>
        UnionSchemaHelpers.GetSchemaName("", typeName, fieldName.SchemaName, "Union");

    public override void WriteTo(Utf8JsonWriter writer, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, HashSet<SchemaName> writtenSchemas, string? containingNamespace) { }
}
