using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public sealed record class UnionTypeSchema(SchemaName SchemaName, CSharpName CSharpName, ImmutableArray<AvroSchema> MemberSchemas)
    : TopLevelSchema(
        SchemaType.UnionType,
        SchemaName,
        CSharpName,
        UnionSchemaHelpers.GetDocumentation(MemberSchemas),
        ImmutableSortedDictionary<string, JsonElement>.Empty)
{
    public static bool CanCreateObjectUnion(UnionSchema union)
    {
        var memberCount = 0;
        // An object union member must be a record, error, fixed not substituted by another type, or null.
        foreach (var schema in union.Schemas)
        {
            switch (schema)
            {
                case { Type: SchemaType.Null }:
                    break;
                case RecordSchema or ErrorSchema or FixedSchema { IsSubstituted: false }:
                    memberCount++;
                    break;
                default:
                    return false;
            }
        }

        return memberCount > 1;
    }

    internal static SchemaName GetSchemaName(SchemaName typeName, FieldName fieldName, bool useUnions) =>
        UnionSchemaHelpers.GetSchemaName(useUnions ? "" : "I", typeName, fieldName.SchemaName, useUnions ? "Union" : "Variant");

    public override void WriteTo(Utf8JsonWriter writer, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, HashSet<SchemaName> writtenSchemas, string? containingNamespace) { }
}
