namespace AvroSourceGenerator.Syntax.Json;

internal closed record class JsonSyntaxNode(JsonSyntaxKind JsonSyntaxKind) : ISyntaxNode
{
    public JsonSyntaxNode? Parent { get; internal set; }

    public abstract SyntaxKind SyntaxKind { get; }

    public ReadOnlySpan<char> GetRawText() => this.GetSourceSpan().AsSpan();

    public abstract IEnumerable<ISyntaxNode> Children();
}
