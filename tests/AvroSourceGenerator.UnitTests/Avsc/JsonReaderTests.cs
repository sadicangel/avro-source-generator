using AvroSourceGenerator.Compiler.Configuration;
using AvroSourceGenerator.Compiler.Parsing;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Syntax.Json;
using AvroSourceGenerator.Syntax;
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
        Assert.Equal("ab", property.Name.ValueText);
        var values = Assert.IsType<JsonArraySyntax>(property.Value).Elements;
        Assert.Equal(Text.IndexOf("1.00", StringComparison.Ordinal), values[0].GetSourceSpan().Offset);
        Assert.Equal("1.00", values[0].AsJsonElement().GetRawText());
        Assert.Equal(["1.00", "true", "null", "{}"], values.Select(value => value.GetRawText().ToString()));
        Assert.IsType<JsonNullSyntax>(values[2]);
        Assert.Equal(Text.IndexOf("{}", StringComparison.Ordinal), values[3].GetSourceSpan().Offset);
    }

    [Fact]
    public void Object_lookup_selects_last_identical_duplicate_and_tracks_both_spans()
    {
        var root = Span("{\"x\":null,\"x\":null,\"\":1}", TestContext.Current.CancellationToken);
        var properties = root.Properties;
        Assert.Equal(["x", ""], properties.Select(property => property.Name.ValueText));
        Assert.NotEqual(Assert.Single(root.Duplicates).Value.GetSourceSpan().Offset, properties[0].Value.GetSourceSpan().Offset);
        Assert.Equal("null", properties[0].Value.GetRawText().ToString());
        Assert.Equal("null", root.Duplicates[0].Value.GetSourceSpan().ToString());
    }

    private static JsonObjectSyntax Span(string text, CancellationToken cancellationToken)
    {
        var tree = Parser.ParseJson(new SourceText("test.avsc", text), cancellationToken);
        Assert.Empty(tree.Diagnostics);
        return Assert.IsType<JsonObjectSyntax>(tree.Root);
    }

    [Fact]
    public void Shared_parser_consumes_one_embedded_value_and_preserves_its_source()
    {
        const string Value = """{ "label": "Ready", "flags": [true, /* note */ false] }""";
        var token = TestContext.Current.CancellationToken;
        var parser = new Parser(new SourceText("test.avdl", Value + " []"), token);
        var obj = Assert.IsType<JsonObjectSyntax>(parser.ParseJsonValue());

        Assert.Equal(Value, obj.GetSourceSpan().ToString());
        Assert.Equal("Ready", obj.AsJsonElement().GetProperty("label").GetString());
        Assert.False(obj.AsJsonElement().GetProperty("flags")[1].GetBoolean());
        var empty = Assert.IsType<JsonArraySyntax>(parser.ParseJsonValue());
        Assert.Equal("[]", empty.GetSourceSpan().ToString());
        Assert.Equal(Value.Length + 1, empty.GetSourceSpan().Offset);

        Assert.Equal(Value, obj.AsJsonElement().GetRawText());

        var strict = Parser.ParseJson(new SourceText("test.avsc", Value), token);
        var diagnostic = Assert.Single(strict.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.JsonCommentNotAllowed, diagnostic.Code);
        Assert.Equal("/* note */", diagnostic.SourceSpan.ToString());
    }

    [Theory]
    [InlineData("test.avsc")]
    [InlineData("test.avpr")]
    public void Scanner_errors_skip_json_grammar_and_the_build_reports_each_error(string path)
    {
        var source = new SourceText(path, "$ // comment with !\r\n{ 1 !");
        var token = TestContext.Current.CancellationToken;
        var tree = Parser.ParseJson(source, token);

        Assert.Same(source, tree.SourceText);
        Assert.IsType<JsonNullSyntax>(tree.Root);
        Assert.Equal(
            [AvroDiagnosticCode.InvalidCharacter, AvroDiagnosticCode.JsonCommentNotAllowed, AvroDiagnosticCode.InvalidCharacter],
            tree.Diagnostics.Select(diagnostic => diagnostic.Code));
        Assert.Equal(["$", "// comment with !", "!"], tree.Diagnostics.Select(diagnostic => diagnostic.SourceSpan.ToString()));

        var file = AvxxParser.Parse(source, new AvroParseOptions(), token);
        Assert.False(file.IsValid);
        Assert.Equal(tree.Diagnostics, file.GetDiagnostics());
    }

    [Theory]
    [InlineData("// comment with \u2028 and !")]
    [InlineData("/* comment */")]
    [InlineData("/** documentation */")]
    [InlineData("/**/")]
    [InlineData("/* unfinished !")]
    [InlineData("/* comment \0 with ! */")]
    public void Disallowed_json_comments_report_the_full_span_once(string comment)
    {
        var source = new SourceText("test.avsc", "{} " + comment);
        var file = AvxxParser.Parse(source, new AvroParseOptions(), TestContext.Current.CancellationToken);

        Assert.False(file.IsValid);
        var diagnostic = Assert.Single(file.GetDiagnostics());
        Assert.Equal(AvroDiagnosticCode.JsonCommentNotAllowed, diagnostic.Code);
        Assert.Empty(diagnostic.Arguments);
        Assert.Equal("Invalid JSON: Comments are not allowed in JSON.", diagnostic.GetMessage());
        Assert.Equal(source.GetSourceSpan(3, comment.Length), diagnostic.SourceSpan);
    }

    [Fact]
    public void Empty_json_returns_a_placeholder_root_and_diagnostic()
    {
        var source = new SourceText("empty.avsc", " \t");
        var tree = Parser.ParseJson(source, TestContext.Current.CancellationToken);
        Assert.IsType<JsonNullSyntax>(tree.Root);
        var diagnostic = Assert.Single(tree.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.EmptyJson, diagnostic.Code);
        Assert.Equal(source.GetSourceSpan(), diagnostic.SourceSpan);
    }

    [Theory]
    [InlineData("Ready")]
    [InlineData("{ label: 1 }")]
    [InlineData("""{ "label": Ready }""")]
    [InlineData("[true,]")]
    [InlineData("[true, /* note */ ]")]
    [InlineData("""{ "label": true, }""")]
    [InlineData("{\"label\":\u00a01}")]
    [InlineData("[// comment\u2028\n1]")]
    [InlineData("\"raw\ttab\"")]
    public void Embedded_json_rejects_non_json_values(string value)
    {
        foreach (var text in new[]
        {
            "schema R; @custom(" + value + ") record R {}",
            "schema R; record R { string f = " + value + "; }"
        })
        {
            var source = new SourceText("test.avdl", text);
            var syntax = Parser.ParseIdl(source, TestContext.Current.CancellationToken);
            Assert.NotEmpty(syntax.Diagnostics);
            var file = AvxxParser.Parse(source, new AvroParseOptions(), TestContext.Current.CancellationToken);
            Assert.False(file.IsValid);
            Assert.Equal(syntax.Diagnostics, file.GetDiagnostics());
        }
    }

    [Theory]
    [InlineData("01")]
    [InlineData("-01")]
    [InlineData(".5")]
    [InlineData("1.")]
    [InlineData("1.e2")]
    public void Invalid_json_number_spelling_is_reported_in_both_formats(string number)
    {
        var standalone = new SourceText("test.avsc", $$"""{"type":"fixed","name":"F","size":{{number}}}""");
        var embedded = new SourceText("test.avdl", "schema R; @custom(" + number + ") record R {}");
        foreach (var source in new[] { standalone, embedded })
        {
            var file = AvxxParser.Parse(source, new AvroParseOptions(), TestContext.Current.CancellationToken);
            var diagnostic = Assert.Single(file.GetDiagnostics());
            Assert.Equal(AvroDiagnosticCode.InvalidNumber, diagnostic.Code);
            Assert.Equal($"Invalid number literal '{number}'", diagnostic.GetMessage());
            Assert.Equal(source.GetSourceSpan(source.Text.IndexOf(number, StringComparison.Ordinal), number.Length), diagnostic.SourceSpan);
        }
    }

    [Fact]
    public void Native_idl_integer_arguments_keep_their_existing_spelling_rules()
    {
        var source = new SourceText("test.avdl", "schema F; fixed F(01);");
        var file = AvxxParser.Parse(source, new AvroParseOptions(), TestContext.Current.CancellationToken);
        Assert.True(file.IsValid);
        Assert.Equal(1, Assert.IsType<FixedSchema>(Assert.Single(file.Declarations)).Size);
    }

    [Theory]
    [InlineData("test.avsc")]
    [InlineData("test.avpr")]
    public void Json_builds_report_each_recovered_grammar_error_in_source_order(string path)
    {
        var source = new SourceText(path, "[01,,2.]");
        var token = TestContext.Current.CancellationToken;
        var tree = Parser.ParseJson(source, token);

        Assert.Collection(tree.Diagnostics,
            diagnostic => Check(diagnostic, AvroDiagnosticCode.InvalidNumber, 1, 2, "Invalid number literal '01'"),
            diagnostic => Check(diagnostic, AvroDiagnosticCode.UnexpectedJsonValue, 4, 1, "JSON value expected"),
            diagnostic => Check(diagnostic, AvroDiagnosticCode.InvalidNumber, 5, 2, "Invalid number literal '2.'"));

        var file = AvxxParser.Parse(source, new AvroParseOptions(), token);
        Assert.False(file.IsValid);
        Assert.Equal(tree.Diagnostics, file.GetDiagnostics());

        void Check(AvroDiagnostic diagnostic, AvroDiagnosticCode code, int offset, int length, string message)
        {
            Assert.Equal(code, diagnostic.Code);
            Assert.Equal(source.GetSourceSpan(offset, length), diagnostic.SourceSpan);
            Assert.Equal(message, diagnostic.GetMessage());
        }
    }

    [Theory]
    [InlineData("{")]
    [InlineData("[")]
    public void A_malformed_object_does_not_consume_the_next_array_element(string delimiter)
    {
        const string Prefix = """{"fields":[{ """;
        var text = Prefix + delimiter + """ "name":"first","type":"int"},{"name":"second","type":"string"}]}""";
        var source = new SourceText("test.avsc", text);
        var tree = Parser.ParseJson(source, TestContext.Current.CancellationToken);

        var diagnostic = Assert.Single(tree.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.UnexpectedToken, diagnostic.Code);
        Assert.Equal(source.GetSourceSpan(Prefix.Length, 1), diagnostic.SourceSpan);
        var root = Assert.IsType<JsonObjectSyntax>(tree.Root);
        var fields = Assert.IsType<JsonArraySyntax>(root.Property("fields")!.Value);
        Assert.Equal(2, fields.Elements.Length);
        Assert.Empty(Assert.IsType<JsonObjectSyntax>(fields.Elements[0]).Properties);
        Assert.Equal("second", Assert.IsType<JsonStringSyntax>(Assert.IsType<JsonObjectSyntax>(fields.Elements[1]).Property("name")!.Value).Value);
    }

    [Theory]
    [InlineData("""{ [ { "ignored": 02 }, [1, 2] ], "second": 01 }""", "[")]
    [InlineData("""{ { "ignored": 02 }, "second": 01 }""", "{")]
    public void Recovery_skips_nested_blocks_and_reports_errors_in_the_next_property(string text, string opener)
    {
        var source = new SourceText("test.avpr", text);
        var file = AvxxParser.Parse(source, new AvroParseOptions(), TestContext.Current.CancellationToken);

        Assert.False(file.IsValid);
        var diagnostics = file.GetDiagnostics().ToArray();
        Assert.Equal([AvroDiagnosticCode.UnexpectedToken, AvroDiagnosticCode.InvalidNumber], diagnostics.Select(diagnostic => diagnostic.Code));
        Assert.Equal([opener, "01"], diagnostics.Select(diagnostic => diagnostic.SourceSpan.ToString()));
        Assert.Equal(text.IndexOf(opener, 1, StringComparison.Ordinal), diagnostics[0].SourceSpan.Offset);
    }

    [Fact]
    public void Recovery_leaves_an_ambiguous_closer_for_the_current_object()
    {
        const string Text = """[{"data":{"type":"array", {"items":"string"}}, {"name":"second"}]""";
        var source = new SourceText("test.avsc", Text);
        var tree = Parser.ParseJson(source, TestContext.Current.CancellationToken);

        var diagnostic = Assert.Single(tree.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.UnexpectedToken, diagnostic.Code);
        Assert.Equal(source.GetSourceSpan(Text.IndexOf("{\"items", StringComparison.Ordinal), 1), diagnostic.SourceSpan);
        var fields = Assert.IsType<JsonArraySyntax>(tree.Root).Elements;
        Assert.Equal(2, fields.Length);
        Assert.Equal("second", Assert.IsType<JsonStringSyntax>(Assert.IsType<JsonObjectSyntax>(fields[1]).Property("name")!.Value).Value);
    }

    [Fact]
    public void Recovery_stops_at_eof_when_the_block_is_unclosed()
    {
        const string Text = """{ { "ignored": [1, 2""";
        var source = new SourceText("test.avsc", Text);
        var tree = Parser.ParseJson(source, TestContext.Current.CancellationToken);

        var diagnostic = Assert.Single(tree.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.UnexpectedToken, diagnostic.Code);
        Assert.Equal(source.GetSourceSpan(Text.IndexOf('{', 1), 1), diagnostic.SourceSpan);
    }

    [Fact]
    public void Recovery_leaves_the_enclosing_array_separator_after_an_unexpected_value_block()
    {
        const string Text = """[(1, { "ignored": 02 }), 01]""";
        var tree = Parser.ParseJson(new SourceText("test.avsc", Text), TestContext.Current.CancellationToken);

        Assert.Equal([AvroDiagnosticCode.UnexpectedJsonValue, AvroDiagnosticCode.InvalidNumber], tree.Diagnostics.Select(diagnostic => diagnostic.Code));
        Assert.Equal(["(", "01"], tree.Diagnostics.Select(diagnostic => diagnostic.SourceSpan.ToString()));
    }

    [Theory]
    [InlineData("{", AvroDiagnosticCode.UnexpectedToken, "")]
    [InlineData("[] []", AvroDiagnosticCode.TrailingJsonContent, "[")]
    [InlineData("[1,]", AvroDiagnosticCode.UnexpectedToken, ",")]
    [InlineData("""{"a":1,}""", AvroDiagnosticCode.UnexpectedToken, ",")]
    [InlineData("[1,,]", AvroDiagnosticCode.UnexpectedJsonValue, ",")]
    [InlineData("""{"a":}""", AvroDiagnosticCode.UnexpectedJsonValue, "}")]
    [InlineData("""{"a":,"b":1}""", AvroDiagnosticCode.UnexpectedJsonValue, ",")]
    [InlineData("""{"a": """, AvroDiagnosticCode.UnexpectedJsonValue, "")]
    [InlineData("""{"a"}""", AvroDiagnosticCode.UnexpectedToken, "}")]
    [InlineData("`name", AvroDiagnosticCode.InvalidCharacter, "`")]
    [InlineData("[ \u00a01]", AvroDiagnosticCode.InvalidCharacter, "\u00a0")]
    [InlineData("[😀]", AvroDiagnosticCode.InvalidCharacter, "😀")]
    [InlineData("\"raw\ttab\"", AvroDiagnosticCode.InvalidCharacter, "\t")]
    [InlineData("{\"type\":\"record\",\"name\":\"R\",\"fields\":false,", AvroDiagnosticCode.UnexpectedToken, "")]
    public void Malformed_documents_report_a_specific_syntax_diagnostic(string text, AvroDiagnosticCode code, string span)
    {
        var file = AvxxParser.Parse(new SourceText("test.avsc", text), new AvroParseOptions(), TestContext.Current.CancellationToken);
        Assert.False(file.IsValid);
        var diagnostic = Assert.Single(file.GetDiagnostics());
        Assert.Equal(code, diagnostic.Code);
        Assert.Equal(span, diagnostic.SourceSpan.ToString());
    }

    [Fact]
    public void Unpaired_surrogate_in_json_property_name_is_reported_at_the_name()
    {
        var source = new SourceText("test.avsc", """{"\uD800": 0}""");
        var tree = Parser.ParseJson(source, TestContext.Current.CancellationToken);

        var diagnostic = Assert.Single(tree.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.JsonPropertyNameUnpairedSurrogate, diagnostic.Code);
        Assert.Empty(diagnostic.Arguments);
        Assert.Equal("Invalid JSON: Property name contains an unpaired Unicode surrogate.", diagnostic.GetMessage());
        Assert.Equal(source.GetSourceSpan(1, 8), diagnostic.SourceSpan);
    }

    [Fact]
    public void Malformed_unicode_json_uses_absolute_utf16_location()
    {
        const string Text = "{\r\n\"é😀\":0,\"bad\":!\r\n}";
        var file = AvxxParser.Parse(new SourceText("test.avsc", Text), new AvroParseOptions(), TestContext.Current.CancellationToken);
        var diagnostic = Assert.Single(file.GetDiagnostics());
        Assert.Equal(AvroDiagnosticCode.InvalidCharacter, diagnostic.Code);
        Assert.Equal(Text.IndexOf('!'), diagnostic.SourceSpan.Offset);
        Assert.Equal("!", diagnostic.SourceSpan.ToString());
        Assert.Equal("Unexpected character '!'", diagnostic.GetMessage());
    }

    [Fact]
    public void Recovering_from_an_invalid_escape_skips_escaped_quotes_in_the_same_string()
    {
        const string Text = """{"bad":"invalid\q then \"quote\" and \\","other":$}""";
        var source = new SourceText("test.avsc", Text);
        var tree = Parser.ParseJson(source, TestContext.Current.CancellationToken);

        Assert.Equal([AvroDiagnosticCode.InvalidEscapeSequence, AvroDiagnosticCode.InvalidCharacter], tree.Diagnostics.Select(diagnostic => diagnostic.Code));
        Assert.Equal(["\\q", "$"], tree.Diagnostics.Select(diagnostic => diagnostic.SourceSpan.ToString()));
    }

    [Fact]
    public void Default_depth_limit_is_preserved()
    {
        var token = TestContext.Current.CancellationToken;
        var limit = new string('[', 64) + "0" + new string(']', 64);
        var valid = Parser.ParseJson(new SourceText("test.avsc", limit), token);
        Assert.Empty(valid.Diagnostics);
        Assert.Equal(limit, valid.Root.AsJsonElement().GetRawText());

        var deep = new SourceText("test.avsc", "[" + limit + "]");
        var diagnostic = Assert.Single(Parser.ParseJson(deep, token).Diagnostics);
        Assert.Equal(AvroDiagnosticCode.JsonNestingLimitExceeded, diagnostic.Code);
        Assert.Equal(64, Assert.IsType<int>(Assert.Single(diagnostic.Arguments)));
        Assert.Equal("Invalid JSON: JSON nesting exceeds 64 levels.", diagnostic.GetMessage());
        Assert.Equal(deep.GetSourceSpan(64, 1), diagnostic.SourceSpan);
    }

    [Fact]
    public void Cancellation_before_parsing_propagates()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var error = Assert.Throws<OperationCanceledException>(() => Parser.ParseJson(new SourceText("test.avsc", "[1,2]"), cancellation.Token));
        Assert.Equal(cancellation.Token, error.CancellationToken);
    }
}
