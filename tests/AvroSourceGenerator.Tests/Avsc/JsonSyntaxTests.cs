using System.Text.Json;
using AvroSourceGenerator.Avsc.Syntax;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Tests.Avsc;

public sealed class JsonSyntaxTests
{
    [Fact]
    public void Eager_children_and_lazy_scalar_values_are_reused()
    {
        const string text = """{"items":["escaped\u0020value",12,true,null,{},[]]}""";
        var root = Assert.IsType<JsonObjectSyntax>(Parse(text, TestContext.Current.CancellationToken));
        var properties = root.Properties;
        Assert.Equal(properties, root.Properties);
        var array = Assert.IsType<JsonArraySyntax>(root.GetProperty("items")!.Value);
        Assert.Same(properties[0].Value, array);
        var items = array.Items;
        Assert.Equal(items, array.Items);
        var value = Assert.IsType<JsonValueSyntax>(items[0]);
        Assert.Equal("escaped value", value.StringValue);
        Assert.Same(value.StringValue, value.StringValue);
        Assert.Equal(12, Assert.IsType<JsonValueSyntax>(items[1]).Int32Value);
        Assert.Equal(items[1].ToJsonElement(), items[1].ToJsonElement());
        Assert.Equal(JsonTokenType.True, items[2].TokenType);
        Assert.Equal(JsonTokenType.Null, items[3].TokenType);
        Assert.Empty(Assert.IsType<JsonObjectSyntax>(items[4]).Properties);
        Assert.Empty(Assert.IsType<JsonArraySyntax>(items[5]).Items);
        Assert.Null(root.GetProperty("missing"));
        Assert.Null(Assert.IsType<JsonValueSyntax>(items[0]).Int32Value);
    }

    [Fact]
    public void Duplicate_properties_preserve_order_occurrences_and_precise_name_spans()
    {
        const string text = "{\r\n\"é😀\":0,\"a\\u0062\" : null,\"ab\":null}";
        var root = Assert.IsType<JsonObjectSyntax>(Parse(text, TestContext.Current.CancellationToken));
        var properties = root.Properties;
        Assert.Equal(new[] { "é😀", "ab" }, properties.Select(property => property.Name.Value));
        Assert.Single(root.Duplicates);
        Assert.Equal("\"a\\u0062\"", root.Duplicates[0].Name.SourceSpan.ToString());
        Assert.Equal(text.IndexOf("\"a\\u0062\"", StringComparison.Ordinal), root.Duplicates[0].Name.SourceSpan.Offset);
        Assert.NotEqual(root.Duplicates[0].Value.SourceSpan, properties[1].Value.SourceSpan);
        Assert.Same(properties[1].Value, root.GetProperty("ab")!.Value);
        Assert.Equal(text.LastIndexOf("null", StringComparison.Ordinal), root.GetProperty("ab")!.Value.SourceSpan.Offset);
    }

    [Fact]
    public void Exact_raw_values_round_trip_without_reconstruction()
    {
        const string text = """ { "custom": [1.00, "\u0061", false] } """;
        var root = Parse(text, TestContext.Current.CancellationToken);
        Assert.Equal(text.Trim(), root.GetRawText());
        Assert.Equal(text.Trim(), root.ToJsonElement().GetRawText());
        var custom = Assert.IsType<JsonObjectSyntax>(root).GetProperty("custom")!.Value;
        Assert.Equal("""[1.00, "\u0061", false]""", custom.ToJsonElement().GetRawText());
        Assert.Equal(custom.GetRawText(), custom.ToJsonElement().GetRawText());
    }

    [Fact]
    public void Parsed_syntax_has_parent_links_to_its_containing_object_or_array()
    {
        const string text = """{"scalar":1,"array":[true,{"nested":[null]}]}""";
        var root = Assert.IsType<JsonObjectSyntax>(Parse(text, TestContext.Current.CancellationToken));

        Assert.Null(root.Parent);

        var scalarProperty = root.GetProperty("scalar")!;
        Assert.Same(root, scalarProperty.Parent);
        Assert.Same(root, scalarProperty.Value.Parent);

        var arrayProperty = root.GetProperty("array")!;
        var array = Assert.IsType<JsonArraySyntax>(arrayProperty.Value);
        Assert.Same(root, arrayProperty.Parent);
        Assert.Same(root, array.Parent);
        Assert.Same(array, array.Items[0].Parent);

        var nestedObject = Assert.IsType<JsonObjectSyntax>(array.Items[1]);
        Assert.Same(array, nestedObject.Parent);

        var nestedProperty = nestedObject.GetProperty("nested")!;
        var nestedArray = Assert.IsType<JsonArraySyntax>(nestedProperty.Value);
        Assert.Same(nestedObject, nestedProperty.Parent);
        Assert.Same(nestedObject, nestedArray.Parent);
        Assert.Same(nestedArray, nestedArray.Items[0].Parent);
    }

    private static JsonSyntax Parse(string text, CancellationToken cancellationToken)
    {
        var reader = new JsonReader(new SourceText("test.avsc", text), cancellationToken);
        Assert.True(reader.Read());
        var root = reader.Parse();
        Assert.False(reader.Read());
        return root;
    }

    [Fact]
    public void Replacements_keep_first_seen_order_and_all_duplicate_spans()
    {
        const string text = """{"a":{"old":1},"b":true,"a":2,"c":[],"a":3,"b":false}""";
        var root = Assert.IsType<JsonObjectSyntax>(Parse(text, TestContext.Current.CancellationToken));
        Assert.Equal(new[] { "a", "b", "c" }, root.Properties.Select(property => property.Name.Value));
        Assert.Equal(new[] { "3", "false", "[]" }, root.Properties.Select(property => property.Value.GetRawText()));
        Assert.Equal(2, root.Duplicates.Count(property => property.Name.Value == "a"));
        Assert.Equal(new[] { """{"old":1}""", "2" }, root.Duplicates.Where(property => property.Name.Value == "a").Select(property => property.Value.SourceSpan.ToString()));
        Assert.Equal(new[] { "true" }, root.Duplicates.Where(property => property.Name.Value == "b").Select(property => property.Value.SourceSpan.ToString()));
        Assert.Equal(text, root.ToJsonElement().GetRawText());
        Assert.Equal(6, root.ToJsonElement().EnumerateObject().Count());
    }

    [Fact]
    public void Every_kind_and_full_container_has_an_exact_span()
    {
        const string text = " \r\n[\"é😀\\u0061\",-1.25e+2,true,false,null,{\"x\":[]},{}] \r\n";
        var root = Assert.IsType<JsonArraySyntax>(Parse(text, TestContext.Current.CancellationToken));
        var expected = new[] { "\"é😀\\u0061\"", "-1.25e+2", "true", "false", "null", "{\"x\":[]}", "{}" };
        Assert.Equal(text.Trim(), root.GetRawText());
        Assert.Equal(3, root.SourceSpan.Offset);
        for (var index = 0; index < expected.Length; index++)
        {
            Assert.Equal(expected[index], root.Items[index].GetRawText());
            Assert.Equal(text.IndexOf(expected[index], StringComparison.Ordinal), root.Items[index].SourceSpan.Offset);
            Assert.Equal(expected[index].Length, root.Items[index].SourceSpan.Length);
        }
        var nested = Assert.IsType<JsonObjectSyntax>(root.Items[5]);
        Assert.Equal("[]", nested.GetProperty("x")!.Value.GetRawText());
    }
}
