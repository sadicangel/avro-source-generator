using System.Text.Json.Nodes;

namespace AvroSourceGenerator.Avdl.Syntax;

internal readonly ref struct JsonParser(SyntaxTokenStream stream)
{
    public static JsonNode? Parse(SyntaxTokenStream stream) => new JsonParser(stream).ParseJson();

    private JsonNode? ParseJson()
    {
        return stream.Current.SyntaxKind switch
        {
            SyntaxKind.NullKeyword => ParseNull(),
            SyntaxKind.TrueKeyword => ParseTrue(),
            SyntaxKind.FalseKeyword => ParseFalse(),
            SyntaxKind.IntegerLiteralToken or SyntaxKind.FloatLiteralToken => ParseNumber(),
            SyntaxKind.StringLiteralToken => ParseString(),
            SyntaxKind.BracketOpenToken => ParseArray(),
            SyntaxKind.BraceOpenToken => ParseObject(),
            SyntaxKind.IdentifierToken => ParseSymbol(),
            _ => throw new InvalidOperationException($"Unexpected token: {stream.Current.SyntaxKind}"),
        };
    }

    private JsonValue? ParseNull()
    {
        _ = stream.Match(SyntaxKind.NullKeyword);
        return null;
    }

    private JsonValue ParseTrue()
    {
        _ = stream.Match(SyntaxKind.TrueKeyword);
        return JsonValue.Create(true);
    }

    private JsonValue ParseFalse()
    {
        _ = stream.Match(SyntaxKind.FalseKeyword);
        return JsonValue.Create(false);
    }

    private JsonValue? ParseNumber()
    {
        var numberToken = stream.Current.SyntaxKind is SyntaxKind.IntegerLiteralToken
            ? stream.Match(SyntaxKind.IntegerLiteralToken)
            : stream.Match(SyntaxKind.FloatLiteralToken);
        return numberToken.Value switch
        {
            int value => JsonValue.Create(value),
            long value => JsonValue.Create(value),
            double value => JsonValue.Create(value),
            _ => JsonValue.Create(numberToken.Value),
        };
    }

    private JsonValue? ParseString()
    {
        var stringToken = stream.Match(SyntaxKind.StringLiteralToken);
        return JsonValue.Create((string?)stringToken.Value);
    }

    private JsonArray ParseArray()
    {
        _ = stream.Match(SyntaxKind.BracketOpenToken);
        var array = new JsonArray();
        while (stream is { IsAtEnd: false, Current.SyntaxKind: not SyntaxKind.BracketCloseToken })
        {
            array.Add(ParseJson());
            if (stream.Current.SyntaxKind is not SyntaxKind.CommaToken)
                break;

            _ = stream.Match(SyntaxKind.CommaToken);
        }

        _ = stream.Match(SyntaxKind.BracketCloseToken);

        return array;
    }

    private JsonObject ParseObject()
    {
        _ = stream.Match(SyntaxKind.BraceOpenToken);
        var @object = new JsonObject();
        while (stream is { IsAtEnd: false, Current.SyntaxKind: not SyntaxKind.BraceCloseToken })
        {
            var propertyName = stream.Current.SyntaxKind is SyntaxKind.StringLiteralToken
                ? stream.Match(SyntaxKind.StringLiteralToken)
                : stream.Match(SyntaxKind.IdentifierToken);
            _ = stream.Match(SyntaxKind.ColonToken);
            var propertyValue = ParseJson();

            @object.Add((string?)propertyName.Value ?? propertyName.ValueText, propertyValue);
            if (stream.Current.SyntaxKind is not SyntaxKind.CommaToken)
                break;

            _ = stream.Match(SyntaxKind.CommaToken);
        }

        _ = stream.Match(SyntaxKind.BraceCloseToken);

        return @object;
    }

    private JsonValue ParseSymbol()
    {
        var symbolToken = stream.Match(SyntaxKind.IdentifierToken);
        return JsonValue.Create(symbolToken.ValueText);
    }
}
