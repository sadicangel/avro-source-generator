using AvroSourceGenerator.Avdl;
using AvroSourceGenerator.Avdl.Declarations;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Tests.Avdl;

public sealed class SemanticResultTests
{
    private static readonly AvroParseOptions Options = new(GenerationTarget.Modern, true);

    [Theory]
    [InlineData("record R {}", AvroDiagnosticCode.InvalidIdlDocument)]
    [InlineData("protocol P {} protocol Q {}", AvroDiagnosticCode.InvalidIdlDocument)]
    [InlineData("schema R; protocol P {}", AvroDiagnosticCode.InvalidIdlDeclaration)]
    [InlineData("schema string;", AvroDiagnosticCode.MissingRootSchema)]
    [InlineData("schema F; fixed F(0);", AvroDiagnosticCode.InvalidIdlFixedSize)]
    [InlineData("schema F; fixed F(-1);", AvroDiagnosticCode.InvalidIdlFixedSize)]
    [InlineData("schema F; fixed F(2147483648);", AvroDiagnosticCode.InvalidIdlFixedSize)]
    [InlineData("schema R; record R { decimal(2147483648, 0) f; }", AvroDiagnosticCode.InvalidIdlDecimalPrecision)]
    [InlineData("schema R; record R { decimal(10, 2147483648) f; }", AvroDiagnosticCode.InvalidIdlDecimalScale)]
    [InlineData("protocol P { string call() oneway; }", AvroDiagnosticCode.InvalidIdlOneWayMessage)]
    [InlineData("protocol P { error E {} void call() oneway throws E; }", AvroDiagnosticCode.InvalidIdlOneWayMessage)]
    [InlineData("protocol P { record P {} }", AvroDiagnosticCode.RecursiveSchemaDefinition)]
    public void Semantic_failures_return_an_invalid_file(string text, AvroDiagnosticCode code)
    {
        var file = Parse(text);
        AssertInvalid(file);
        Assert.Equal(code, Assert.Single(file.Diagnostics).Code);
    }

    [Theory]
    [InlineData("@namespace(null) record R {}")]
    [InlineData("@aliases(null) record R {}")]
    [InlineData("@aliases([\"Old\", null]) record R {}")]
    [InlineData("@aliases([\"Old\", 2]) record R {}")]
    [InlineData("enum R { A } = false;")]
    [InlineData("record R { @logicalType(null) string f; }")]
    [InlineData("record R { string @order(null) f; }")]
    public void Invalid_values_preserve_the_annotation_or_default_diagnostic(string declaration)
    {
        var file = Parse("schema R; " + declaration);
        AssertInvalid(file);
        Assert.Equal(AvroDiagnosticCode.InvalidIdlDeclaration, Assert.Single(file.Diagnostics).Code);
    }

    [Theory]
    [InlineData("record R { array<@logicalType(1) string> f; }")]
    [InlineData("record R { map<@logicalType(1) string> f; }")]
    [InlineData("record R { union { null, @logicalType(1) string } f; }")]
    [InlineData("record R { array<@logicalType(1) string>? f; }")]
    [InlineData("error R { @logicalType(1) string f; }")]
    public void Invalid_nested_types_prevent_parent_construction(string declaration)
    {
        var file = Parse("schema R; " + declaration);
        AssertInvalid(file);
        Assert.Equal("1", Assert.Single(file.Diagnostics).SourceSpan.ToString());
    }

    [Fact]
    public void Independent_errors_are_collected_in_semantic_traversal_order()
    {
        const string text = """
            schema R;
            @aliases(false) fixed F(0);
            @aliases(1) record R {
                @logicalType(2) string @aliases(3) @order(4) first;
                union { @logicalType(5) string, @logicalType(6) long } second;
                decimal(2147483648, 2147483649) third;
            }
            enum E { A } = 7;
            """;
        var file = Parse(text);
        AssertInvalid(file);
        Assert.Equal(
            ["false", "0", "1", "2", "3", "4", "5", "6", "2147483648", "2147483649", "7"],
            file.Diagnostics.Select(diagnostic => diagnostic.SourceSpan.ToString()));
        Assert.All(file.Diagnostics, diagnostic => Assert.Equal("test.avdl", diagnostic.SourceSpan.SourceText.Path.OriginalPath));
        Assert.Equal(file.Diagnostics, Parse(text).Diagnostics);
    }

    [Theory]
    [InlineData("fixed F(0);")]
    [InlineData("enum F { A } = 3;")]
    [InlineData("record F { @logicalType(3) string f; }")]
    [InlineData("error F { @logicalType(3) string f; }")]
    public void Invalid_names_stop_validation_of_the_node_but_not_later_siblings(string declaration)
    {
        var file = Parse($"schema F; @namespace(1) @aliases(2) {declaration} fixed G(-1);");
        AssertInvalid(file);
        Assert.Equal(["1", "-1"], file.Diagnostics.Select(diagnostic => diagnostic.SourceSpan.ToString()));
    }

    [Fact]
    public void Invalid_protocol_name_stops_child_validation()
    {
        var file = Parse("@namespace(1) protocol P { fixed F(0); string call() oneway; }");
        AssertInvalid(file);
        var diagnostic = Assert.Single(file.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.InvalidIdlDeclaration, diagnostic.Code);
        Assert.Equal("1", diagnostic.SourceSpan.ToString());
    }

    [Fact]
    public void Invalid_declarations_do_not_hide_later_declarations_or_main_type_errors()
    {
        var file = Parse("schema @logicalType(3) string; protocol P {} fixed F(0);");
        AssertInvalid(file);
        Assert.Equal(
            [AvroDiagnosticCode.InvalidIdlDeclaration, AvroDiagnosticCode.InvalidIdlFixedSize, AvroDiagnosticCode.InvalidIdlDeclaration],
            file.Diagnostics.Select(diagnostic => diagnostic.Code));
        Assert.Equal(["protocol P {}", "0", "3"], file.Diagnostics.Select(diagnostic => diagnostic.SourceSpan.ToString()));
    }

    [Fact]
    public void Protocol_errors_continue_across_types_parameters_responses_and_messages()
    {
        const string text = """
            protocol P {
                fixed F(0);
                decimal(2147483648, 0) first(@logicalType(1) string a, @logicalType(2) string b) oneway;
                string second(@logicalType(4) string c) oneway;
                void third(@logicalType(5) string d);
            }
            """;
        var file = Parse(text);
        AssertInvalid(file);
        Assert.Equal(["0", "1", "2", "2147483648", "4", "oneway", "5"], file.Diagnostics.Select(diagnostic => diagnostic.SourceSpan.ToString()));
        // The first response failed, so its one-way rule cannot be evaluated.
        Assert.Single(file.Diagnostics, diagnostic => diagnostic.Code == AvroDiagnosticCode.InvalidIdlOneWayMessage);
    }

    [Fact]
    public void Failed_schema_scope_does_not_poison_later_siblings()
    {
        var file = Parse("protocol P { record P {} fixed F(0); fixed F(1); }");
        AssertInvalid(file);
        Assert.Equal([AvroDiagnosticCode.RecursiveSchemaDefinition, AvroDiagnosticCode.InvalidIdlFixedSize], file.Diagnostics.Select(diagnostic => diagnostic.Code));
    }

    [Fact]
    public void Recursive_declarations_stop_before_metadata_validation()
    {
        const string text = "protocol P { @aliases(1) fixed P(0); }";
        var file = Parse(text);
        AssertInvalid(file);
        var diagnostic = Assert.Single(file.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.RecursiveSchemaDefinition, diagnostic.Code);
        Assert.Equal("P", diagnostic.SourceSpan.ToString());
        Assert.Equal(text.LastIndexOf('P'), diagnostic.SourceSpan.Offset);
    }

    [Fact]
    public void Syntax_errors_skip_semantic_validation()
    {
        const string text = "schema R; record R { string f } fixed F(0);";
        var source = new SourceText("test.avdl", text);
        var syntax = AvdlParser.Parse(source, TestContext.Current.CancellationToken);
        var file = AvxxParser.Parse(source, Options, TestContext.Current.CancellationToken);
        AssertInvalid(file);
        Assert.Equal(syntax.Diagnostics, file.Diagnostics);
        Assert.DoesNotContain(file.Diagnostics, diagnostic => diagnostic.Code == AvroDiagnosticCode.InvalidIdlFixedSize);
    }

    [Fact]
    public void Static_and_instance_entrypoints_return_the_same_diagnostics()
    {
        var source = new SourceText("test.avdl", "schema R; fixed F(0); record R { @logicalType(1) string f; }");
        var expected = AvxxParser.Parse(source, Options, TestContext.Current.CancellationToken);
        var actual = new AvdlParser(source, Options, TestContext.Current.CancellationToken).ParseFile();
        AssertInvalid(actual);
        Assert.Equal(expected.Diagnostics, actual.Diagnostics);
    }

    [Fact]
    public void Embedded_json_duplicates_keep_the_last_value_in_syntax_and_semantics()
    {
        const string text = """
            schema R;
            @custom({"x": 1, "x": 2})
            record R { map<long> f = {"x": 3, "x": 4}; }
            """;
        var source = new SourceText("test.avdl", text);
        var syntax = AvdlParser.Parse(source, TestContext.Current.CancellationToken);
        Assert.Empty(syntax.Diagnostics);
        var recordSyntax = Assert.IsType<RecordDeclarationSyntax>(Assert.Single(syntax.Document.Declarations));
        Assert.Equal(4, recordSyntax.Fields[0].DefaultValueClause!.JsonValue.JsonNode!["x"]!.GetValue<int>());
        var file = AvxxParser.Parse(source, Options, TestContext.Current.CancellationToken);
        Assert.True(file.IsValid);
        var record = Assert.IsType<RecordSchema>(Assert.Single(file.Declarations));
        Assert.Equal(2, record.Properties["custom"].GetProperty("x").GetInt32());
        Assert.Equal(4, record.Fields[0].DefaultJson!.Value.GetProperty("x").GetInt32());
    }

    [Theory]
    [InlineData("1e999")]
    [InlineData("-1e999")]
    public void Overflowing_numbers_return_diagnostics_in_syntax_and_file_parsing(string number)
    {
        var source = new SourceText("test.avdl", $"schema R; @custom({number}) record R {{ double f = {number}; }}");
        var syntax = AvdlParser.Parse(source, TestContext.Current.CancellationToken);
        var file = AvxxParser.Parse(source, Options, TestContext.Current.CancellationToken);
        AssertInvalid(file);
        Assert.Equal(syntax.Diagnostics, file.Diagnostics);
        var numbers = file.Diagnostics.Where(diagnostic => diagnostic.Code == AvroDiagnosticCode.InvalidNumber).ToArray();
        Assert.Equal(2, numbers.Length);
        Assert.All(numbers, diagnostic => Assert.Equal(number, diagnostic.SourceSpan.ToString()));
    }

    [Fact]
    public void Null_enum_default_and_empty_annotations_keep_their_existing_meaning()
    {
        var file = Parse("schema E; @namespace(\"\") @aliases([]) enum E { A } = null;");
        Assert.True(file.IsValid);
        var enumeration = Assert.IsType<EnumSchema>(Assert.Single(file.Declarations));
        Assert.Empty(enumeration.Aliases);
    }

    [Fact]
    public void File_parsing_propagates_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => AvxxParser.Parse(new SourceText("test.avdl", "schema R; record R {}"), Options, cancellation.Token));
    }

    private static AvroFile Parse(string text) =>
        AvxxParser.Parse(new SourceText("test.avdl", text), Options, TestContext.Current.CancellationToken);

    private static void AssertInvalid(AvroFile file)
    {
        Assert.False(file.IsValid);
        Assert.NotEmpty(file.Diagnostics);
        Assert.Same(AvroSchema.Null, file.RootSchema);
        Assert.Empty(file.Declarations);
        Assert.Empty(file.DeclarationSpans);
        Assert.Empty(file.References);
        Assert.Empty(file.ReferenceSpans);
        Assert.Empty(file.Dependencies);
        Assert.Empty(file.Imports);
    }
}
