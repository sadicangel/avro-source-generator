using System.Text.Json;
using AvroSourceGenerator.Compiler.Configuration;
using AvroSourceGenerator.Compiler.Parsing;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Schemas.Defaults;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Serialization;
using AvroSourceGenerator.Syntax.Declarations;
using AvroSourceGenerator.Syntax;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.UnitTests.Avdl;

public sealed class SemanticResultTests
{
    private static readonly AvroParseOptions s_options = new AvroParseOptions(GenerationTarget.Modern, LanguageFeatures.NullableReferenceTypes);

    [Theory]
    [InlineData("schema R; record Item {} record R { @custom(42) Item Value; }", "custom")]
    [InlineData("schema R; record Item {} record R { @custom(42) Item? Value; }", "custom")]
    [InlineData("namespace example; schema R; record Item {} record R { map<array<@custom(42) example.Item>> Values; }", "custom")]
    [InlineData("schema R; record Item {} record R { union { null, @custom(42) @order(\"ignore\") Item } Value; @custom(43) Item Other; }", "custom", "order", "custom")]
    [InlineData("schema @custom(42) Item; record Item {}", "custom")]
    [InlineData("protocol P { record Item {} void send(@custom(42) Item value); }", "custom")]
    public void Named_reference_annotations_report_errors_at_each_annotation(string text, params string[] annotations)
    {
        var source = new SourceText("schemas/reference.avdl", text);
        var token = TestContext.Current.CancellationToken;
        var options = new AvroParseOptions(GenerationTarget.Apache, LanguageFeatures.NullableReferenceTypes);
        var tree = Parser.ParseIdl(source, token);
        var file = AvroFile.Parse(source, options, token);
        var compilation = AvroCompiler.Compile([source], options, cancellationToken: token);

        AssertInvalid(file);
        Assert.False(compilation.IsValid);
        Assert.Equal(tree.Diagnostics, file.GetDiagnostics());
        Assert.Equal(tree.Diagnostics, compilation.Diagnostics);
        Assert.Equal(annotations.Length, compilation.Diagnostics.Length);
        var offset = 0;
        foreach (var (index, annotation) in annotations.Index())
        {
            offset = text.IndexOf($"@{annotation}(", offset, StringComparison.Ordinal);
            var diagnostic = compilation.Diagnostics[index];
            Assert.Equal(AvroDiagnosticCode.MisplacedAnnotation, diagnostic.Code);
            Assert.Equal(AvroDiagnosticSeverity.Error, diagnostic.Severity);
            Assert.Same(source, diagnostic.SourceSpan.SourceText);
            Assert.Equal(new SourceSpan(source, offset, 1), diagnostic.SourceSpan);
            Assert.Equal($"Annotation '@{annotation}' is not permitted on a named type reference", diagnostic.GetMessage());
            offset++;
        }
    }

    [Fact]
    public void Supported_annotations_keep_their_metadata_in_the_serialized_schema()
    {
        var source = new SourceText("schemas/reference.avdl", """
            schema R;
            @custom(1) record Item {}
            record R {
                Item @custom(2) Value;
                @custom(3) array<Item> Items;
                map<@custom(4) string> Names;
            }
            """);
        var options = new AvroParseOptions(GenerationTarget.Apache, LanguageFeatures.NullableReferenceTypes);
        var compilation = AvroCompiler.Compile([source], options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(compilation.IsValid);
        Assert.Empty(compilation.Diagnostics);
        var root = Assert.Single(compilation.Files).RootSchema;
        Assert.NotNull(root);
        using var json = JsonDocument.Parse(root.ToAvjs(compilation.Schemas));
        var fields = json.RootElement.GetProperty("fields");
        Assert.Equal(1, fields[0].GetProperty("type").GetProperty("custom").GetInt32());
        Assert.Equal(2, fields[0].GetProperty("custom").GetInt32());
        Assert.Equal(3, fields[1].GetProperty("type").GetProperty("custom").GetInt32());
        Assert.Equal(4, fields[2].GetProperty("type").GetProperty("values").GetProperty("custom").GetInt32());
    }

    [Theory]
    [InlineData("record R {}", AvroDiagnosticCode.InvalidIdlDocument)]
    [InlineData("protocol P {} protocol Q {}", AvroDiagnosticCode.InvalidIdlDocument)]
    [InlineData("schema R; protocol P {}", AvroDiagnosticCode.InvalidIdlDeclaration)]
    [InlineData("schema F; fixed F(0);", AvroDiagnosticCode.InvalidFixedSize)]
    [InlineData("schema F; fixed F(-1);", AvroDiagnosticCode.InvalidFixedSize)]
    [InlineData("schema F; fixed F(2147483648);", AvroDiagnosticCode.InvalidFixedSize)]
    [InlineData("schema R; record R { decimal(2147483648, 0) f; }", AvroDiagnosticCode.InvalidDecimalPrecision)]
    [InlineData("schema R; record R { decimal(10, 2147483648) f; }", AvroDiagnosticCode.InvalidDecimalScale)]
    [InlineData("protocol P { string call() oneway; }", AvroDiagnosticCode.InvalidOneWayMessage)]
    [InlineData("protocol P { error E {} void call() oneway throws E; }", AvroDiagnosticCode.InvalidOneWayMessage)]
    [InlineData("protocol P { record P {} }", AvroDiagnosticCode.RecursiveSchemaDefinition)]
    public void Semantic_failures_return_an_invalid_file(string text, AvroDiagnosticCode code)
    {
        var file = Parse(text);
        AssertInvalid(file);
        Assert.Equal(code, Assert.Single(file.GetDiagnostics()).Code);
    }

    [Theory]
    [InlineData("@aliases([\"Old\", null]) record R {}", AvroDiagnosticCode.InvalidAliases)]
    [InlineData("@aliases([\"Old\", 2]) record R {}", AvroDiagnosticCode.InvalidAliases)]
    [InlineData("enum R { A } = false;", AvroDiagnosticCode.InvalidEnumDefault)]
    [InlineData("record R { @logicalType(null) string f; }", AvroDiagnosticCode.InvalidLogicalType)]
    public void Invalid_values_have_property_specific_diagnostics(string declaration, AvroDiagnosticCode code)
    {
        var file = Parse("schema R; " + declaration);
        AssertInvalid(file);
        Assert.Equal(code, Assert.Single(file.GetDiagnostics()).Code);
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
        Assert.Equal("1", Assert.Single(file.GetDiagnostics()).SourceSpan.ToString());
    }

    [Fact]
    public void Errors_before_a_failed_field_and_in_later_declarations_keep_traversal_order()
    {
        const string Text = """
            schema R;
            @aliases(false) fixed F(0);
            @aliases(1) record R {
                @logicalType(2) string @aliases(3) @order(4) first;
                union { @logicalType(5) string, @logicalType(6) long } second;
                decimal(2147483648, 2147483649) third;
            }
            enum E { A } = 7;
            """;
        var file = Parse(Text);
        AssertInvalid(file);
        Assert.Equal(
            ["false", "0", "1", "2", "3", "4", "7"],
            file.GetDiagnostics().Select(diagnostic => diagnostic.SourceSpan.ToString()));
        Assert.Equal(
            [AvroDiagnosticCode.InvalidAliases, AvroDiagnosticCode.InvalidFixedSize, AvroDiagnosticCode.InvalidAliases,
                AvroDiagnosticCode.InvalidLogicalType, AvroDiagnosticCode.InvalidAliases, AvroDiagnosticCode.InvalidOrder,
                AvroDiagnosticCode.InvalidEnumDefault],
            file.GetDiagnostics().Select(diagnostic => diagnostic.Code));
        Assert.All(file.GetDiagnostics(), diagnostic => Assert.Equal("test.avdl", diagnostic.SourceSpan.SourceText.Path.OriginalPath));
        Assert.Equal(file.GetDiagnostics(), Parse(Text).GetDiagnostics());
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
        Assert.Equal(["1", "-1"], file.GetDiagnostics().Select(diagnostic => diagnostic.SourceSpan.ToString()));
    }

    [Fact]
    public void Invalid_protocol_name_stops_child_validation()
    {
        var file = Parse("@namespace(1) protocol P { fixed F(0); string call() oneway; }");
        AssertInvalid(file);
        var diagnostic = Assert.Single(file.GetDiagnostics());
        Assert.Equal(AvroDiagnosticCode.InvalidNamespace, diagnostic.Code);
        Assert.Equal("1", diagnostic.SourceSpan.ToString());
    }

    [Fact]
    public void Invalid_declarations_do_not_hide_later_declarations_or_main_type_errors()
    {
        var file = Parse("schema @logicalType(3) string; protocol P {} fixed F(0);");
        AssertInvalid(file);
        Assert.Equal(
            [AvroDiagnosticCode.InvalidIdlDeclaration, AvroDiagnosticCode.InvalidFixedSize, AvroDiagnosticCode.InvalidLogicalType],
            file.GetDiagnostics().Select(diagnostic => diagnostic.Code));
        Assert.Equal(["protocol P {}", "0", "3"], file.GetDiagnostics().Select(diagnostic => diagnostic.SourceSpan.ToString()));
    }

    [Fact]
    public void Protocol_components_report_errors_but_failed_collections_stop_at_the_first_invalid_item()
    {
        const string Text = """
            protocol P {
                fixed F(0);
                decimal(2147483648, 0) first(@logicalType(1) string a, @logicalType(2) string b) oneway;
                string second(@logicalType(4) string c) oneway;
                void third(@logicalType(5) string d);
            }
            """;
        var file = Parse(Text);
        AssertInvalid(file);
        Assert.Equal(["0", "1", "2147483648"], file.GetDiagnostics().Select(diagnostic => diagnostic.SourceSpan.ToString()));
        // The response failed, so its one-way rule cannot be evaluated; later messages are skipped.
        Assert.DoesNotContain(file.GetDiagnostics(), diagnostic => diagnostic.Code == AvroDiagnosticCode.InvalidOneWayMessage);
    }

    [Fact]
    public void Protocol_types_stop_after_a_failed_schema_scope()
    {
        var file = Parse("protocol P { record P {} fixed F(0); fixed F(1); }");
        AssertInvalid(file);
        Assert.Equal([AvroDiagnosticCode.RecursiveSchemaDefinition], file.GetDiagnostics().Select(diagnostic => diagnostic.Code));
    }

    [Fact]
    public void Recursive_declarations_stop_before_metadata_validation()
    {
        const string Text = "protocol P { @aliases(1) fixed P(0); }";
        var file = Parse(Text);
        AssertInvalid(file);
        var diagnostic = Assert.Single(file.GetDiagnostics());
        Assert.Equal(AvroDiagnosticCode.RecursiveSchemaDefinition, diagnostic.Code);
        Assert.Equal("P", diagnostic.SourceSpan.ToString());
        Assert.Equal(Text.LastIndexOf('P'), diagnostic.SourceSpan.Offset);
    }

    [Fact]
    public void Syntax_errors_skip_semantic_validation()
    {
        const string Text = "schema R; record R { string f } fixed F(0);";
        var source = new SourceText("test.avdl", Text);
        var syntax = Parser.ParseIdl(source, TestContext.Current.CancellationToken);
        var file = AvxxParser.Parse(source, s_options, TestContext.Current.CancellationToken);
        AssertInvalid(file);
        Assert.Equal(syntax.Diagnostics, file.GetDiagnostics());
        Assert.DoesNotContain(file.GetDiagnostics(), diagnostic => diagnostic.Code == AvroDiagnosticCode.InvalidFixedSize);
    }

    [Theory]
    [InlineData("namespace Example; schema R; record R { string value; }")]
    [InlineData("protocol P { import idl \"common.avdl\"; void ping(); }")]
    [InlineData("schema R; fixed F(0); record R { @logicalType(1) string f; }")]
    [InlineData("schema R; record R { string f } fixed F(0);")]
    [InlineData("schema R; schema S; record R {}")]
    [InlineData("record R {}")]
    public void Lowering_a_parsed_tree_matches_file_parsing(string text)
    {
        var source = new SourceText("test.avdl", text);
        var token = TestContext.Current.CancellationToken;
        var syntax = Parser.ParseIdl(source, token);
        var (expected, expectedRootSpan) = AvdlParser.ParseFile(source, s_options, token);
        var (actual, actualRootSpan) = new AvdlParser(syntax, s_options, token).ParseFile();
        var expectedSchemas = expected.Declarations.ToDictionary(schema => schema.SchemaName);
        var actualSchemas = actual.Declarations.ToDictionary(schema => schema.SchemaName);

        Assert.Same(source, actual.Text);
        Assert.Equal(expected.IsValid, actual.IsValid);
        Assert.Equal(expected.ParseOptions, actual.ParseOptions);
        Assert.Equal(expected.RootSchema.ToAvjs(expectedSchemas), actual.RootSchema.ToAvjs(actualSchemas));
        Assert.Equal(expected.Declarations.Select(schema => schema.ToAvjs(expectedSchemas)), actual.Declarations.Select(schema => schema.ToAvjs(actualSchemas)));
        Assert.Equal(expected.DeclarationSpans, actual.DeclarationSpans);
        Assert.Equal(expected.References, actual.References);
        Assert.Equal(expected.Imports, actual.Imports);
        Assert.Equal(expected.GetDiagnostics(), actual.GetDiagnostics());
        Assert.Equal(expectedRootSpan, actualRootSpan);
    }

    [Fact]
    public void Embedded_json_duplicates_keep_the_last_value_in_syntax_and_semantics()
    {
        const string Text = """
            schema R;
            @custom({"x": 1, "x": 2})
            record R { map<long> f = {"x": 3, "x": 4}; }
            """;
        var source = new SourceText("test.avdl", Text);
        var syntax = Parser.ParseIdl(source, TestContext.Current.CancellationToken);
        Assert.Empty(syntax.Diagnostics);
        var recordSyntax = Assert.IsType<RecordDeclarationSyntax>(Assert.Single(syntax.Root.Declarations));
        Assert.Equal(4, recordSyntax.Fields[0].DefaultValueClause!.JsonValue.AsJsonElement().GetProperty("x").GetInt32());
        var file = AvxxParser.Parse(source, s_options, TestContext.Current.CancellationToken);
        Assert.True(file.IsValid);
        var record = Assert.IsType<RecordSchema>(Assert.Single(file.Declarations));
        Assert.Equal(2, record.Properties["custom"].GetProperty("x").GetInt32());
        var bound = Assert.IsType<RecordSchema>(SchemaCompilerTestHelpers.Bind(file).Declarations.Single());
        using var boundDefault = JsonDocument.Parse(Assert.IsType<UnboundDefaultValue>(bound.Fields[0].Default).JsonText);
        Assert.Equal(4, boundDefault.RootElement.GetProperty("x").GetInt32());
    }

    [Fact]
    public void Embedded_json_preserves_raw_numbers_strings_and_duplicate_properties()
    {
        const string Value = """{ "n": 1.234567890123456789e+00, "s": "\u0061", "n": 2.00 }""";
        var source = new SourceText("test.avdl",
            "schema R; @custom(" + Value + ") record R { map<string> f = " + Value + "; }");
        var file = AvxxParser.Parse(source, s_options, TestContext.Current.CancellationToken);
        Assert.True(file.IsValid);
        Assert.Empty(file.GetDiagnostics());
        var record = Assert.IsType<RecordSchema>(Assert.Single(file.Declarations));
        using var fieldDefault = JsonDocument.Parse(Assert.IsType<UnboundDefaultValue>(record.Fields[0].Default).JsonText);
        var values = new[] { record.Properties["custom"], fieldDefault.RootElement };
        foreach (var value in values)
        {
            Assert.Equal(Value, value.GetRawText());
            Assert.Equal(3, value.EnumerateObject().Count());
            Assert.Equal("1.234567890123456789e+00", value.EnumerateObject().First().Value.GetRawText());
            Assert.Equal("a", value.GetProperty("s").GetString());
            Assert.Equal("2.00", value.GetProperty("n").GetRawText());
        }
    }

    [Theory]
    [InlineData("1e999")]
    [InlineData("-1e999")]
    [InlineData("9223372036854775808")]
    [InlineData("-9223372036854775809")]
    public void Large_numbers_remain_raw_in_metadata_and_defaults(string number)
    {
        var source = new SourceText("test.avdl", $"schema R; @custom({number}) record R {{ double f = {number}; }}");
        var syntax = Parser.ParseIdl(source, TestContext.Current.CancellationToken);
        var file = AvxxParser.Parse(source, s_options, TestContext.Current.CancellationToken);
        Assert.Empty(syntax.Diagnostics);
        Assert.True(file.IsValid);
        Assert.Empty(file.GetDiagnostics());
        var record = Assert.IsType<RecordSchema>(Assert.Single(file.Declarations));
        Assert.Equal(number, record.Properties["custom"].GetRawText());
        Assert.Equal(number, Assert.IsType<UnboundDefaultValue>(record.Fields[0].Default).JsonText);
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
        Assert.Throws<OperationCanceledException>(() => AvxxParser.Parse(new SourceText("test.avdl", "schema R; record Item {} record R { @custom(42) Item Value; }"), s_options, cancellation.Token));
    }

    private static AvroFile Parse(string text) =>
        AvxxParser.Parse(new SourceText("test.avdl", text), s_options, TestContext.Current.CancellationToken);

    private static void AssertInvalid(AvroFile file)
    {
        Assert.False(file.IsValid);
        Assert.NotEmpty(file.GetDiagnostics());
        Assert.Same(AvroSchema.Null, file.RootSchema);
        Assert.Empty(file.Declarations);
        Assert.Empty(file.DeclarationSpans);
        Assert.Empty(file.References);
        Assert.Empty(file.ReferenceSpans);
        Assert.Empty(file.Dependencies);
        Assert.Empty(file.Imports);
    }
}
