using AvroSourceGenerator.Avdl;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.UnitTests.Compiler;

public sealed class ParserProvenanceTests
{
    private static readonly AvroParseOptions s_options = new AvroParseOptions(GenerationTarget.Modern, true);

    [Fact]
    public void Imports_require_source_spans_and_preserve_duplicate_occurrences()
    {
        Assert.Throws<ArgumentException>(() => new AvroImport(AvroImportKind.Idl, "a.avdl", SourceSpan.None));
        const string Text = "import idl \"a.avdl\"; import idl \"a.avdl\"; schema string;";
        var file = AvxxParser.Parse(new SourceText("test.avdl", Text), s_options, TestContext.Current.CancellationToken);
        Assert.True(file.IsValid);
        Assert.Equal(2, file.Imports.Length);
        Assert.All(file.Imports, import => Assert.Equal("\"a.avdl\"", import.SourceSpan.ToString()));
        Assert.True(file.Imports[0].SourceSpan.Offset < file.Imports[1].SourceSpan.Offset);
    }

    [Fact]
    public void Provenance_does_not_change_semantic_schema_equality()
    {
        var first = AvxxParser.Parse(new SourceText("a.avdl", "schema F; fixed F(4);"), s_options, TestContext.Current.CancellationToken);
        var second = AvxxParser.Parse(new SourceText("b.avdl", "\n schema F; fixed F(4);"), s_options, TestContext.Current.CancellationToken);
        Assert.Equal(first.RootSchema, second.RootSchema);
        Assert.Equal(first.Declarations, second.Declarations);
        Assert.NotEqual(first.DeclarationSpans, second.DeclarationSpans);
    }

    [Fact]
    public void Recursive_definition_diagnostic_points_to_the_nested_name()
    {
        const string Text = "protocol P { record P {} }";
        var file = AvxxParser.Parse(new SourceText("test.avdl", Text), s_options, TestContext.Current.CancellationToken);
        var diagnostic = Assert.Single(file.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.RecursiveSchemaDefinition, diagnostic.Code);
        Assert.Equal("P", diagnostic.SourceSpan.ToString());
        Assert.Equal(Text.LastIndexOf('P'), diagnostic.SourceSpan.Offset);
        Assert.Equal("Recursive schema definition detected for 'P'.", diagnostic.GetMessage());
    }

    [Fact]
    public void Syntax_nodes_return_the_union_of_their_token_spans_or_none()
    {
        Assert.True(AvdlParser.Parse(new SourceText("empty.avdl", ""), TestContext.Current.CancellationToken).Document.GetSourceSpan().IsNone);

        const string Text = "/** documentation */ record R {}";
        var declaration = Assert.Single(AvdlParser.Parse(new SourceText("test.avdl", Text), TestContext.Current.CancellationToken).Document.Declarations);
        Assert.Equal("record R {}", declaration.GetSourceSpan().ToString());
    }

    [Fact]
    public void Declaration_occurrences_and_ordered_reference_uses_have_separate_provenance()
    {
        const string Text = "namespace ns; schema ns.R; record R { ns.Missing a; ns.Missing b; } record R {}";
        var file = AvroFile.Parse(new SourceText("test.avdl", Text), s_options, TestContext.Current.CancellationToken);
        Assert.True(file.IsValid, string.Join("; ", file.Diagnostics));
        Assert.Equal(
            ["record R { ns.Missing a; ns.Missing b; }", "record R {}"],
            file.DeclarationSpans.Select(span => span.ToString()));
        Assert.Equal(Text.LastIndexOf("record", StringComparison.Ordinal), file.DeclarationSpans[1].Offset);
        var uses = file.ReferenceSpans[new SchemaName("Missing", "ns")];
        Assert.Equal(["ns.Missing", "ns.Missing"], uses.Select(span => span.ToString()));
        Assert.True(uses[0].Offset < uses[1].Offset);
        Assert.Equal(Text.IndexOf("ns.R", StringComparison.Ordinal), Assert.Single(file.ReferenceSpans[new SchemaName("R", "ns")]).Offset);
    }

    [Fact]
    public void Duplicate_diagnostic_points_to_later_occurrence()
    {
        const string Text = "schema R; record R {} record R {}";
        var compilation = Bind(("test.avdl", Text));
        var diagnostic = Assert.Single(compilation.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.DuplicateSchema, diagnostic.Code);
        Assert.Equal(Text.LastIndexOf("record", StringComparison.Ordinal), diagnostic.SourceSpan.Offset);
        Assert.Equal("record R {}", diagnostic.SourceSpan.ToString());
    }

    [Fact]
    public void Cross_file_duplicate_points_to_the_later_file()
    {
        var diagnostic = Assert.Single(
            Bind(
                ("a.avdl", "schema R; record R {}"),
                ("b.avdl", "schema R; record R {}")).Diagnostics);
        Assert.Equal("b.avdl", diagnostic.SourceSpan.SourceText.Path.OriginalPath);
        Assert.Equal("record R {}", diagnostic.SourceSpan.ToString());
    }

    [Fact]
    public void Missing_references_stay_grouped_and_anchor_in_source_order()
    {
        const string Text = "schema R; record R { Z first; A second; Z third; }";
        var diagnostic = Assert.Single(Bind(("test.avdl", Text)).Diagnostics);
        Assert.Equal(AvroDiagnosticCode.MissingReferences, diagnostic.Code);
        Assert.Equal(Text.IndexOf('Z'), diagnostic.SourceSpan.Offset);
        Assert.Equal("Z", diagnostic.SourceSpan.ToString());
        Assert.Contains("'A', 'Z'", diagnostic.GetMessage());
    }

    [Theory]
    [InlineData("import idl \"missing.avdl\"; schema R; record R {}", "\"missing.avdl\"")]
    [InlineData("protocol P { import schema \"wrong.avdl\"; }", "\"wrong.avdl\"")]
    public void Unused_import_warnings_point_to_the_literal(string text, string literal)
    {
        var diagnostic = Assert.Single(Bind(("test.avdl", text)).Diagnostics);
        Assert.Equal(AvroDiagnosticCode.UnusedImport, diagnostic.Code);
        Assert.Equal(literal, diagnostic.SourceSpan.ToString());
        Assert.Equal(text.IndexOf(literal, StringComparison.Ordinal), diagnostic.SourceSpan.Offset);
    }

    [Fact]
    public void Cycle_points_to_the_closing_import_edge()
    {
        var diagnostic = Assert.Single(
            Bind(
                ("a.avdl", "import idl \"b.avdl\"; schema A; record A { B b; }"),
                ("b.avdl", "import idl \"a.avdl\"; schema B; record B { A a; }")).Diagnostics);
        Assert.Equal("b.avdl", diagnostic.SourceSpan.SourceText.Path.OriginalPath);
        Assert.Equal("\"a.avdl\"", diagnostic.SourceSpan.ToString());
        Assert.Contains("a.avdl -> b.avdl -> a.avdl", diagnostic.GetMessage());
    }

    [Theory]
    [InlineData("schema F; fixed F(0);", "0")]
    [InlineData("protocol P { string call() oneway; }", "oneway")]
    public void Semantic_validation_returns_a_precise_diagnostic(string text, string anchor)
    {
        var result = AvxxParser.Parse(new SourceText("test.avdl", text), s_options, TestContext.Current.CancellationToken);
        Assert.False(result.IsValid);
        Assert.Equal(anchor, Assert.Single(result.Diagnostics).SourceSpan.ToString());
    }

    [Fact]
    public void Syntax_validation_returns_diagnostics()
    {
        var result = AvxxParser.Parse(new SourceText("test.avdl", "schema ;"), s_options, TestContext.Current.CancellationToken);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Diagnostics);
    }

    [Theory]
    [InlineData("test.avsc", "{\"type\":\"record\",\"name\":\"R\",\"fields\":null}", AvroDiagnosticCode.InvalidFields)]
    [InlineData("test.avpr", "{\"protocol\":\"P\",\"types\":null,\"messages\":{}}", AvroDiagnosticCode.InvalidTypes)]
    public void Json_validation_returns_precise_value_diagnostics(string path, string text, AvroDiagnosticCode code)
    {
        var source = new SourceText(path, text);
        var result = AvxxParser.Parse(source, s_options, TestContext.Current.CancellationToken);
        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(code, diagnostic.Code);
        var offset = text.IndexOf("null", StringComparison.Ordinal);
        Assert.Equal(source.GetSourceSpan(offset, "null".Length), diagnostic.SourceSpan);
        Assert.Contains("must be an array", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    private static AvroCompilation Bind(params (string Path, string Text)[] sources) =>
        SchemaCompilerTestHelpers.Bind(ReferenceResolution.Deferred, DuplicateResolution.Error, sources);
}
