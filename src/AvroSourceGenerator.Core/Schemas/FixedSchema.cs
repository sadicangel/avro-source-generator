using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Extensions;

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

    public override void WriteTo(Utf8JsonWriter writer, IReadOnlyDictionary<SchemaName, TopLevelSchema> registeredSchemas, HashSet<SchemaName> writtenSchemas, string? containingNamespace)
    {
        if (!writtenSchemas.Add(SchemaName))
        {
            writer.WriteStringValue(SchemaName.RelativeTo(containingNamespace));
            return;
        }

        writer.WriteStartObject();
        writer.WriteString(AvroJsonKeys.Type, AvroTypeNames.Fixed);
        writer.WriteString(AvroJsonKeys.Name, SchemaName.Name);
        var @namespace = SchemaName.Namespace ?? containingNamespace;
        if (@namespace is not null)
            writer.WriteString(AvroJsonKeys.Namespace, @namespace);
        if (Documentation is not null)
            writer.WriteString(AvroJsonKeys.Doc, Documentation);
        if (Aliases.Length > 0)
        {
            writer.WriteStartArray(AvroJsonKeys.Aliases);
            foreach (var alias in Aliases)
                writer.WriteStringValue(alias);
            writer.WriteEndArray();
        }

        writer.WriteNumber(AvroJsonKeys.Size, Size);
        foreach (var entry in Properties)
        {
            writer.WritePropertyName(entry.Key);
            entry.Value.WriteTo(writer);
        }

        writer.WriteEndObject();
    }
}
