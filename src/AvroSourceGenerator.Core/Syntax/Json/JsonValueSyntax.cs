using System.Text.Json;

namespace AvroSourceGenerator.Syntax.Json;

internal closed record class JsonValueSyntax(JsonSyntaxKind JsonSyntaxKind) : JsonSyntaxNode(JsonSyntaxKind)
{
    private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip };

    private JsonElement? _element;

    public JsonElement AsJsonElement() => _element ??= JsonSerializer.Deserialize<JsonElement>(this.GetSourceSpan().AsSpan(), s_jsonOptions);
}
