using System.Globalization;

namespace AvroSourceGenerator.Syntax.Json;

internal sealed record class JsonNumberSyntax(SyntaxToken Literal) : JsonValueSyntax(JsonSyntaxKind.Number)
{
    public int? Int32Value => int.TryParse(Literal.SourceSpan.AsSpan(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number) ? number : null;

    public override SyntaxKind SyntaxKind => SyntaxKind.JsonNumber;

    public override IEnumerable<ISyntaxNode> Children() { yield return Literal; }
}
