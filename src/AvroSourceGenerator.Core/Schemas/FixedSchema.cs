using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Schemas;

public sealed record class FixedSchema(
    SchemaName SchemaName,
    string? Documentation,
    ImmutableArray<string> Aliases,
    int Size,
    ImmutableSortedDictionary<string, JsonElement> Properties)
    : NamedSchema(SchemaType.Fixed, SchemaName, Documentation, Aliases, Properties)
{
    public bool IsSubstituted => !AreSameName(SchemaName.Name, CSharpName.Name) || !AreSameNamespace(SchemaName.Namespace, CSharpName.Namespace);

    private static bool AreSameName(ReadOnlySpan<char> schema, ReadOnlySpan<char> csharp) =>
        csharp.StartsWith('@') ? schema.SequenceEqual(csharp[1..]) : schema.SequenceEqual(csharp);

    private static bool AreSameNamespace(ReadOnlySpan<char> schema, ReadOnlySpan<char> csharp)
    {
        if (!csharp.Contains('@'))
            return schema.SequenceEqual(csharp);

        var schemaParts = new SplitEnumerable.Enumerator(schema, '.');
        var csharpParts = new SplitEnumerable.Enumerator(csharp, '.');

        while (true)
        {
            var schemaHasNext = schemaParts.MoveNext();
            var csharpHasNext = csharpParts.MoveNext();
            if (!schemaHasNext && !csharpHasNext)
                return true;
            if (!schemaHasNext || !csharpHasNext || !AreSameName(schemaParts.Current, csharpParts.Current))
                return false;
        }
    }
}
