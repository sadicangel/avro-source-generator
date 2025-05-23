using System.Text.Json.Nodes;
using AvroSourceGenerator.AvroIDL.Syntax;
using AvroSourceGenerator.AvroIDL.Syntax.Declarations;

namespace AvroSourceGenerator.AvroIDL.Parsing;

partial class Parser
{
    private static JsonValueSyntax ParseJsonValue(SyntaxTree syntaxTree, SyntaxIterator iterator)
    {
        var startTokenIndex = iterator.Index;
        var json = JsonParser.ParseJson(iterator);
        var endTokenIndex = iterator.Index;

        return new JsonValueSyntax(
            syntaxTree,
            new SyntaxList<SyntaxToken>([.. iterator.Tokens.Skip(startTokenIndex).Take(endTokenIndex - startTokenIndex)]),
            json);
    }
}

file static class JsonParser
{
    public static JsonNode? ParseJson(SyntaxIterator iterator)
    {
        return iterator.Current.SyntaxKind switch
        {
            SyntaxKind.NullKeyword => ParseNull(iterator),
            SyntaxKind.TrueKeyword => ParseTrue(iterator),
            SyntaxKind.FalseKeyword => ParseFalse(iterator),
            SyntaxKind.IntegerLiteralToken or SyntaxKind.FloatLiteralToken => ParseNumber(iterator),
            SyntaxKind.StringLiteralToken => ParseString(iterator),
            SyntaxKind.BracketOpenToken => ParseArray(iterator),
            SyntaxKind.BraceOpenToken => ParseObject(iterator),
            SyntaxKind.IdentifierToken => ParseSymbol(iterator),
            _ => throw new InvalidOperationException($"Unexpected token: {iterator.Current.SyntaxKind}"),
        };
    }

    public static JsonValue? ParseNull(SyntaxIterator iterator)
    {
        _ = iterator.Match(SyntaxKind.NullKeyword);
        return null;
    }

    public static JsonValue? ParseTrue(SyntaxIterator iterator)
    {
        _ = iterator.Match(SyntaxKind.NullKeyword);
        return JsonValue.Create(true);
    }

    public static JsonValue? ParseFalse(SyntaxIterator iterator)
    {
        _ = iterator.Match(SyntaxKind.NullKeyword);
        return JsonValue.Create(false);
    }

    public static JsonValue? ParseNumber(SyntaxIterator iterator)
    {
        var numberToken = iterator.Match(SyntaxKind.IntegerLiteralToken, SyntaxKind.FloatLiteralToken);
        return JsonValue.Create(numberToken.Value);
    }

    public static JsonValue? ParseString(SyntaxIterator iterator)
    {
        var stringToken = iterator.Match(SyntaxKind.StringLiteralToken);
        return JsonValue.Create((string?)stringToken.Value);
    }

    public static JsonArray ParseArray(SyntaxIterator iterator)
    {
        _ = iterator.Match(SyntaxKind.BracketOpenToken);
        var array = new JsonArray();
        while (iterator.Current.SyntaxKind is not SyntaxKind.BracketCloseToken)
        {
            array.Add(ParseJson(iterator));
        }
        _ = iterator.Match(SyntaxKind.BracketCloseToken);

        return array;
    }

    public static JsonObject ParseObject(SyntaxIterator iterator)
    {
        _ = iterator.Match(SyntaxKind.BraceOpenToken);
        var @object = new JsonObject();
        while (iterator.Current.SyntaxKind is not SyntaxKind.BraceCloseToken)
        {
            var propertyName = iterator.Match(SyntaxKind.IdentifierToken);
            _ = iterator.Match(SyntaxKind.ColonToken);
            var propertyValue = ParseJson(iterator);

            @object.Add(propertyName.SourceSpan.ToString(), propertyValue);
        }
        _ = iterator.Match(SyntaxKind.BraceCloseToken);

        return @object;
    }

    public static JsonValue? ParseSymbol(SyntaxIterator iterator)
    {
        var symbolToken = iterator.Match(SyntaxKind.IdentifierToken);
        return JsonValue.Create(symbolToken.SourceSpan.ToString());
    }
}
