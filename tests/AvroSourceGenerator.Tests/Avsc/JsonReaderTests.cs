using System.Text.Json;
using AvroSourceGenerator.Avsc.Syntax;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Tests.Avsc;

public sealed class JsonReaderTests
{
    [Fact]
    public void Structural_parse_preserves_absolute_unicode_offsets_and_raw_values()
    {
        const string text = "{\r\n\"é😀\":\"x\",\"nested\":{\"a\\u0062\":[1.00,true,null,{}]}}";
        var root = Span(text, TestContext.Current.CancellationToken);
        var nested = Assert.IsType<JsonObjectSyntax>(root.Properties[1].Value);
        var property = Assert.Single(nested.Properties);
        Assert.Equal("ab", property.Name.Value);
        var values = Assert.IsType<JsonArraySyntax>(property.Value).Items;
        Assert.Equal(text.IndexOf("1.00", StringComparison.Ordinal), values[0].SourceSpan.Offset);
        Assert.Equal("1.00", values[0].ToJsonElement().GetRawText());
        Assert.Equal(["1.00", "true", "null", "{}"], values.Select(value => value.GetRawText()));
        Assert.Equal(JsonTokenType.Null, values[2].TokenType);
        Assert.Equal(text.IndexOf("{}", StringComparison.Ordinal), values[3].SourceSpan.Offset);
    }

    [Fact]
    public void Object_lookup_selects_last_identical_duplicate_and_tracks_both_spans()
    {
        var root = Span("{\"x\":null,\"x\":null,\"\":1}", TestContext.Current.CancellationToken);
        var properties = root.Properties;
        Assert.Equal(["x", ""], properties.Select(property => property.Name.Value));
        Assert.NotEqual(Assert.Single(root.Duplicates).Value.SourceSpan.Offset, properties[0].Value.SourceSpan.Offset);
        Assert.Equal("null", properties[0].Value.GetRawText());
        Assert.Equal("null", root.Duplicates[0].Value.SourceSpan.ToString());
    }

    [Theory]
    [InlineData("{")]
    [InlineData("[] []")]
    [InlineData("[1,]")]
    [InlineData("{\"type\":\"record\",\"name\":\"R\",\"fields\":false,")]
    public void Malformed_documents_return_only_invalid_json(string text)
    {
        var file = AvscParser.ParseFile(new SourceText("test.avsc", text), new AvroParseOptions(), TestContext.Current.CancellationToken);
        Assert.False(file.IsValid);
        Assert.Equal(AvroDiagnosticCode.InvalidJson, Assert.Single(file.Diagnostics).Code);
    }

    [Fact]
    public void Malformed_unicode_json_uses_absolute_utf16_location()
    {
        const string text = "{\r\n\"é😀\":0,\"bad\":!\r\n}";
        var file = AvscParser.ParseFile(new SourceText("test.avsc", text), new AvroParseOptions(), TestContext.Current.CancellationToken);
        var diagnostic = Assert.Single(file.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.InvalidJson, diagnostic.Code);
        Assert.Equal(text.IndexOf('!'), diagnostic.SourceSpan.Offset);
    }

    [Fact]
    public void Default_depth_limit_is_preserved()
    {
        var text = new string('[', 65) + "0" + new string(']', 65);
        var file = AvscParser.ParseFile(new SourceText("test.avsc", text), new AvroParseOptions(), TestContext.Current.CancellationToken);
        Assert.Equal(AvroDiagnosticCode.InvalidJson, Assert.Single(file.Diagnostics).Code);
    }

    [Fact]
    public void Cancellation_between_tokens_and_before_parsing_propagates()
    {
        using var cancellation = new CancellationTokenSource();
        var reader = new JsonReader(new SourceText("test.avsc", "[1,2]"), cancellation.Token);
        Assert.True(reader.Read());
        cancellation.Cancel();
        try
        {
            reader.Parse();
            Assert.Fail("Expected cancellation.");
        }
        catch (OperationCanceledException ex)
        {
            Assert.Equal(cancellation.Token, ex.CancellationToken);
        }
    }

    private static JsonObjectSyntax Span(string text, CancellationToken cancellationToken)
    {
        var reader = new JsonReader(new SourceText("test.avsc", text), cancellationToken);
        Assert.True(reader.Read());
        var root = Assert.IsType<JsonObjectSyntax>(reader.Parse());
        Assert.False(reader.Read());
        return root;
    }
}
