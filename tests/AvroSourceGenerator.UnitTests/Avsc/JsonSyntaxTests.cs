using AvroSourceGenerator.Syntax.Json;
using AvroSourceGenerator.Syntax;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.UnitTests.Avsc;

public sealed class JsonSyntaxTests
{
    [Fact]
    public void Eager_children_and_lazy_scalar_values_are_reused()
    {
        const string Text = """{"items":["escaped\u0020value",12,true,null,{},[]]}""";
        var root = Assert.IsType<JsonObjectSyntax>(Parse(Text, TestContext.Current.CancellationToken));
        var properties = root.Properties;
        Assert.Equal(properties, root.Properties);
        var array = Assert.IsType<JsonArraySyntax>(root.Property("items")!.Value);
        Assert.Same(properties[0].Value, array);
        var items = array.Elements;
        Assert.Equal(items, array.Elements);
        var value = Assert.IsType<JsonStringSyntax>(items[0]);
        Assert.Equal("escaped value", value.Value);
        Assert.Same(value.Literal.ValueText, value.Value);
        Assert.Equal(12, Assert.IsType<JsonNumberSyntax>(items[1]).Int32Value);
        Assert.Equal(items[1].AsJsonElement(), items[1].AsJsonElement());
        Assert.True(Assert.IsType<JsonBooleanSyntax>(items[2]).Value);
        Assert.IsType<JsonNullSyntax>(items[3]);
        Assert.Empty(Assert.IsType<JsonObjectSyntax>(items[4]).Properties);
        Assert.Empty(Assert.IsType<JsonArraySyntax>(items[5]).Elements);
        Assert.Null(root.Property("missing"));
    }

    [Theory]
    [InlineData("\"😀\"", "😀")]
    [InlineData("\"\\uD83D\\uDE00\"", "😀")]
    [InlineData("\"\\uD800\"", null)]
    [InlineData("\"\\uDC00\"", null)]
    public void Text_reads_preserve_unicode_and_raw_spelling(string text, string? decoded)
    {
        var value = Assert.IsType<JsonStringSyntax>(Parse(text, TestContext.Current.CancellationToken));
        Assert.Equal(decoded, value.Value);
        Assert.Equal(text, value.AsJsonElement().GetRawText());
    }

    [Fact]
    public void Duplicate_properties_preserve_order_occurrences_and_precise_name_spans()
    {
        const string Text = "{\r\n\"é😀\":0,\"a\\u0062\" : null,\"ab\":null}";
        var root = Assert.IsType<JsonObjectSyntax>(Parse(Text, TestContext.Current.CancellationToken));
        var properties = root.Properties;
        Assert.Equal(["é😀", "ab"], properties.Select(property => property.Name.ValueText));
        Assert.Single(root.Duplicates);
        Assert.Equal("\"a\\u0062\"", root.Duplicates[0].Name.SourceSpan.ToString());
        Assert.Equal(Text.IndexOf("\"a\\u0062\"", StringComparison.Ordinal), root.Duplicates[0].Name.SourceSpan.Offset);
        Assert.NotEqual(root.Duplicates[0].Value.GetSourceSpan(), properties[1].Value.GetSourceSpan());
        Assert.Same(properties[1].Value, root.Property("ab")!.Value);
        Assert.Equal(Text.LastIndexOf("null", StringComparison.Ordinal), root.Property("ab")!.Value.GetSourceSpan().Offset);
    }

    [Fact]
    public void Exact_raw_values_round_trip_without_reconstruction()
    {
        const string Text = """ { "custom": [1.00, "\u0061", false] } """;
        var root = Parse(Text, TestContext.Current.CancellationToken);
        Assert.Equal(Text.Trim(), root.GetRawText().ToString());
        Assert.Equal(Text.Trim(), root.AsJsonElement().GetRawText());
        var custom = Assert.IsType<JsonObjectSyntax>(root).Property("custom")!.Value;
        Assert.Equal("""[1.00, "\u0061", false]""", custom.AsJsonElement().GetRawText());
        Assert.Equal(custom.GetRawText().ToString(), custom.AsJsonElement().GetRawText());
    }

    private static JsonValueSyntax Parse(string text, CancellationToken cancellationToken)
    {
        var tree = Parser.ParseJson(new SourceText("test.avsc", text), cancellationToken);
        Assert.Empty(tree.Diagnostics);
        return tree.Root;
    }

    [Fact]
    public void Replacements_keep_first_seen_order_and_all_duplicate_spans()
    {
        const string Text = """{"a":{"old":1},"b":true,"a":2,"c":[],"a":3,"b":false}""";
        var root = Assert.IsType<JsonObjectSyntax>(Parse(Text, TestContext.Current.CancellationToken));
        Assert.Equal(
            ["{", "\"a\":{\"old\":1}", "\"b\":true", "\"a\":2", "\"c\":[]", "\"a\":3", "\"b\":false", "}"],
            root.Children().Select(child => child.GetSourceSpan().ToString()));
        Assert.Equal(["a", "b", "c"], root.Properties.Select(property => property.Name.ValueText));
        Assert.Equal(["3", "false", "[]"], root.Properties.Select(property => property.Value.GetRawText().ToString()));
        Assert.Equal(2, root.Duplicates.Count(property => property.Name.ValueText == "a"));
        Assert.Equal(["""{"old":1}""", "2"], root.Duplicates.Where(property => property.Name.ValueText == "a").Select(property => property.Value.GetSourceSpan().ToString()));
        Assert.Equal(["true"], root.Duplicates.Where(property => property.Name.ValueText == "b").Select(property => property.Value.GetSourceSpan().ToString()));
        Assert.Equal(Text, root.AsJsonElement().GetRawText());
        Assert.Equal(6, root.AsJsonElement().EnumerateObject().Count());
    }

    [Fact]
    public void Every_kind_and_full_container_has_an_exact_span()
    {
        const string Text = " \r\n[\"é😀\\u0061\",-1.25e+2,true,false,null,{\"x\":[]},{}] \r\n";
        var root = Assert.IsType<JsonArraySyntax>(Parse(Text, TestContext.Current.CancellationToken));
        var expected = new[] { "\"é😀\\u0061\"", "-1.25e+2", "true", "false", "null", "{\"x\":[]}", "{}" };
        Assert.Equal(Text.Trim(), root.GetRawText().ToString());
        Assert.Equal(3, root.GetSourceSpan().Offset);
        for (var index = 0; index < expected.Length; index++)
        {
            Assert.Equal(expected[index], root.Elements[index].GetRawText().ToString());
            Assert.Equal(Text.IndexOf(expected[index], StringComparison.Ordinal), root.Elements[index].GetSourceSpan().Offset);
            Assert.Equal(expected[index].Length, root.Elements[index].GetSourceSpan().Length);
        }
        var nested = Assert.IsType<JsonObjectSyntax>(root.Elements[5]);
        Assert.Equal("[]", nested.Property("x")!.Value.GetRawText().ToString());
    }
}
