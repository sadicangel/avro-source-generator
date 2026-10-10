using System.Collections.Immutable;

namespace AvroSourceGenerator.Schemas;

internal static class DocumentationHelper
{
    public static string GetDocumentation(ImmutableArray<AvroSchema> memberSchemas, bool includeNull = false) => string.Join(
        """


        """,
        [
            "Represents a union of the following types:",
            "<list type=\"bullet\">",
            .. memberSchemas.Canonicalize(includeNull).Select(static schema => $"<item>{GetSeeElement(schema)}</item>"),
            "</list>"
        ]);

    private static string GetSeeElement(AvroSchema schema) => schema switch
    {
        PrimitiveSchema { SchemaType: SchemaType.Null } => "<see langword=\"null\"/>",
        PrimitiveSchema { SchemaType: SchemaType.Bytes } => "<see cref=\"byte\"/>[]",
        ArraySchema array => $"<see cref=\"global::System.Collections.Generic.List{{T}}\">List</see>&lt;{GetSeeElement(array.ItemSchema)}&gt;",
        MapSchema map => $"<see cref=\"global::System.Collections.Generic.Dictionary{{TKey, TValue}}\">Dictionary</see>&lt;<see cref=\"string\"/>, {GetSeeElement(map.ValueSchema)}&gt;",
        UnionSchema { SchemaType: not SchemaType.UnionType } union => string.Join(" | ", union.Schemas.Canonicalize(includeNull: true).Select(GetSeeElement)),
        _ => $"<see cref=\"{schema.CSharpName.FullName.Replace('<', '{').Replace('>', '}')}\"/>"
    };
}
