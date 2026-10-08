using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public sealed record class UnionObjectSchema(SchemaName SchemaName, CSharpName CSharpName, ImmutableArray<AvroSchema> MemberSchemas)
    : TopLevelSchema(
        SchemaType.UnionObject,
        SchemaName,
        CSharpName,
        UnionSchemaHelpers.GetDocumentation(MemberSchemas),
        ImmutableSortedDictionary<string, JsonElement>.Empty)
{
    public static bool CanCreate(UnionSchema union)
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

    internal static SchemaName GetSchemaName(SchemaName typeName, FieldName fieldName) =>
        UnionSchemaHelpers.GetSchemaName("I", typeName, fieldName.SchemaName, "Variant");

    public override void WriteTo(Utf8JsonWriter writer, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, HashSet<SchemaName> writtenSchemas, string? containingNamespace) { }
}
