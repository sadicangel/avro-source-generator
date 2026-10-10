using System.Collections.Immutable;

namespace AvroSourceGenerator.Schemas;

internal static class UnionSchemaHelpers
{
    public static SchemaName GetSchemaName(string prefix, SchemaName typeName, string fieldName, string suffix)
    {
        var length = prefix.Length + typeName.Name.Length + fieldName.Length + suffix.Length;
        var name = string.Create(
            length,
            (prefix, fieldName, typeName.Name, suffix),
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

    public static string GetDocumentation(ImmutableArray<AvroSchema> memberSchemas, bool includeNull = false) => string.Join(
        """


        """,
        [
            "Represents a union of the following types:",
            "<list type=\"bullet\">",
            .. memberSchemas.Canonicalize(includeNull)
                .Select(static schema => schema.SchemaType is SchemaType.Null
                    ? "<item><see langword=\"null\"/></item>"
                    : $"<item><see cref=\"{schema.CSharpName.FullName}\"/></item>"),
            "</list>"
        ]);
}
