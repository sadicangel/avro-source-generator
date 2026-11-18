using System.Text.Json;

namespace AvroSourceGenerator.Syntax.Json;

internal closed record class JsonValueSyntax(JsonSyntaxKind JsonSyntaxKind) : IJsonSyntaxNode
{
    private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip };

    private JsonElement? _element;

    public abstract SyntaxKind SyntaxKind { get; }

    public abstract IEnumerable<ISyntaxNode> Children();

    public JsonElement AsJsonElement() => _element ??= JsonSerializer.Deserialize<JsonElement>(this.GetSourceSpan().AsSpan(), s_jsonOptions);

    public ReadOnlySpan<char> GetRawText() => this.GetSourceSpan().AsSpan();
}
