using System.Text.Json;
using AvroSourceGenerator.Avjs;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.UnitTests.Avsc;

public sealed class JsonReaderTests
{
    [Fact]
    public void Structural_parse_preserves_absolute_unicode_offsets_and_raw_values()
    {
        const string Text = "{\r\n\"é😀\":\"x\",\"nested\":{\"a\\u0062\":[1.00,true,null,{}]}}";
        var root = Span(Text, TestContext.Current.CancellationToken);
        var nested = Assert.IsType<JsonObjectSyntax>(root.Properties[1].Value);
        var property = Assert.Single(nested.Properties);
        Assert.Equal("ab", property.Name.Value);
        var values = Assert.IsType<JsonArraySyntax>(property.Value).Items;
        Assert.Equal(Text.IndexOf("1.00", StringComparison.Ordinal), values[0].SourceSpan.Offset);
        Assert.Equal("1.00", values[0].AsJsonElement().GetRawText());
        Assert.Equal(["1.00", "true", "null", "{}"], values.Select(value => value.GetRawText()));
        Assert.Equal(JsonTokenType.Null, values[2].TokenType);
        Assert.Equal(Text.IndexOf("{}", StringComparison.Ordinal), values[3].SourceSpan.Offset);
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
        var file = AvxxParser.Parse(new SourceText("test.avsc", text), new AvroParseOptions(), TestContext.Current.CancellationToken);
        Assert.False(file.IsValid);
        var diagnostic = Assert.Single(file.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.InvalidJson, diagnostic.Code);
        Assert.StartsWith("Invalid JSON: ", diagnostic.GetMessage());
        Assert.DoesNotContain("LineNumber:", diagnostic.GetMessage());
        Assert.DoesNotContain("BytePositionInLine:", diagnostic.GetMessage());
    }

    [Fact]
    public void Malformed_unicode_json_uses_absolute_utf16_location()
    {
        const string Text = "{\r\n\"é😀\":0,\"bad\":!\r\n}";
        var file = AvxxParser.Parse(new SourceText("test.avsc", Text), new AvroParseOptions(), TestContext.Current.CancellationToken);
        var diagnostic = Assert.Single(file.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.InvalidJson, diagnostic.Code);
        Assert.Equal(Text.IndexOf('!'), diagnostic.SourceSpan.Offset);
        Assert.Equal("!", diagnostic.SourceSpan.ToString());
        Assert.Equal("Invalid JSON: '!' is an invalid start of a value.", diagnostic.GetMessage());
    }

    [Fact]
    public void Default_depth_limit_is_preserved()
    {
        var text = new string('[', 65) + "0" + new string(']', 65);
        var file = AvxxParser.Parse(new SourceText("test.avsc", text), new AvroParseOptions(), TestContext.Current.CancellationToken);
        Assert.Equal(AvroDiagnosticCode.InvalidJson, Assert.Single(file.Diagnostics).Code);
    }

    [Fact]
    public void Cancellation_between_tokens_and_before_parsing_propagates()
    {
        using var cancellation = new CancellationTokenSource();
        var reader = new JsonReader(new SourceText("test.avsc", "[1,2]"));
        Assert.True(reader.Read());
        cancellation.Cancel();
        try
        {
            reader.Parse(cancellation.Token);
            Assert.Fail("Expected cancellation.");
        }
        catch (OperationCanceledException ex)
        {
            Assert.Equal(cancellation.Token, ex.CancellationToken);
        }
    }

    private static JsonObjectSyntax Span(string text, CancellationToken cancellationToken)
    {
        var reader = new JsonReader(new SourceText("test.avsc", text));
        Assert.True(reader.Read());
        var root = Assert.IsType<JsonObjectSyntax>(reader.Parse(cancellationToken));
        Assert.False(reader.Read());
        return root;
    }
}
