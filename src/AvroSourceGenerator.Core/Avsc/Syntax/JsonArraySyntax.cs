using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Avsc.Syntax;

internal sealed record class JsonArraySyntax(SourceSpan SourceSpan, ImmutableArray<JsonSyntax> Items)
    : JsonSyntax(SourceSpan, JsonTokenType.StartArray)
{
    public ImmutableArray<JsonSyntax>.Enumerator GetEnumerator() => Items.GetEnumerator();
}
