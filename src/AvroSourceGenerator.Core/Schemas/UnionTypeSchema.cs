using System.Collections.Immutable;
using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

public sealed record class UnionTypeSchema(SchemaName SchemaName, CSharpName CSharpName, ImmutableArray<AvroSchema> MemberSchemas)
    : TopLevelSchema(
        SchemaType.UnionType,
        SchemaName,
        CSharpName,
        DocumentationHelper.GetDocumentation(MemberSchemas),
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
                case { SchemaType: SchemaType.Null }:
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

    internal static SchemaName GetSchemaName(SchemaName typeName, FieldName fieldName, bool useUnions)
    {
        var (prefix, suffix) = useUnions ? ("", "Union") : ("I", "Variant");
        var name = string.Create(
            prefix.Length + typeName.Name.Length + fieldName.SchemaName.Length + suffix.Length,
            (prefix, fieldName.SchemaName, typeName.Name, suffix),
            static (span, state) =>
            {
                var (prefix, fieldName, typeName, suffix) = state;
                prefix.AsSpan().CopyTo(span);
                span = span[prefix.Length..];
                typeName.AsSpan().CopyTo(span);
                span = span[typeName.Length..];
                fieldName.AsSpan().CopyTo(span);
                span[0] = char.ToUpperInvariant(span[0]);
                span = span[fieldName.Length..];
                suffix.AsSpan().CopyTo(span);
            });

        return new SchemaName(name, typeName.Namespace);
    }

    public override void WriteTo(Utf8JsonWriter writer, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, HashSet<SchemaName> writtenSchemas, string? containingNamespace) { }
}
