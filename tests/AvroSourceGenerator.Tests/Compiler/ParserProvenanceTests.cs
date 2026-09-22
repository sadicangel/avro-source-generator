using AvroSourceGenerator.Avdl.Syntax;
using AvroSourceGenerator.Avsc.Syntax;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Tests.Compiler;

public sealed class ParserProvenanceTests
{
    private static readonly AvroParseOptions Options = new AvroParseOptions(GenerationTarget.Modern, true);

    [Fact]
    public void Imports_require_source_spans_and_preserve_duplicate_occurrences()
    {
        Assert.Throws<ArgumentException>(() => new AvroImport(AvroImportKind.Idl, "a.avdl", SourceSpan.None));
        const string text = "import idl \"a.avdl\"; import idl \"a.avdl\"; schema string;";
        var file = AvdlParser.ParseFile(new SourceText("test.avdl", text), Options, TestContext.Current.CancellationToken);
        Assert.True(file.IsValid);
        Assert.Equal(2, file.Imports.Length);
        Assert.All(file.Imports, import => Assert.Equal("\"a.avdl\"", import.SourceSpan.ToString()));
        Assert.True(file.Imports[0].SourceSpan.Offset < file.Imports[1].SourceSpan.Offset);
    }

    [Fact]
    public void Provenance_does_not_change_semantic_schema_equality()
    {
        var first = AvdlParser.ParseFile(new SourceText("a.avdl", "schema F; fixed F(4);"), Options, TestContext.Current.CancellationToken);
        var second = AvdlParser.ParseFile(new SourceText("b.avdl", "\n schema F; fixed F(4);"), Options, TestContext.Current.CancellationToken);
        Assert.Equal(first.RootSchema, second.RootSchema);
        Assert.Equal(first.Declarations, second.Declarations);
        Assert.NotEqual(first.DeclarationSpans, second.DeclarationSpans);
    }

    [Fact]
    public void Recursive_definition_diagnostic_points_to_the_nested_name()
    {
        const string text = "protocol P { record P {} }";
        var file = AvdlParser.ParseFile(new SourceText("test.avdl", text), Options, TestContext.Current.CancellationToken);
        var diagnostic = Assert.Single(file.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.RecursiveSchemaDefinition, diagnostic.Code);
        Assert.Equal("P", diagnostic.SourceSpan.ToString());
        Assert.Equal(text.LastIndexOf('P'), diagnostic.SourceSpan.Offset);
        Assert.Equal("Recursive schema definition detected for 'P'.", diagnostic.GetMessage());
    }

    [Fact]
    public void Syntax_nodes_return_the_union_of_their_token_spans_or_none()
    {
        Assert.True(AvdlParser.Parse(new SourceText("empty.avdl", ""), TestContext.Current.CancellationToken).Document.GetSourceSpan().IsNone);

        const string text = "/** documentation */ record R {}";
        var declaration = Assert.Single(AvdlParser.Parse(new SourceText("test.avdl", text), TestContext.Current.CancellationToken).Document.Declarations);
        Assert.Equal("record R {}", declaration.GetSourceSpan().ToString());
    }

    [Fact]
    public void Declaration_occurrences_and_ordered_reference_uses_have_separate_provenance()
    {
        const string text = "namespace ns; schema ns.R; record R { ns.Missing a; ns.Missing b; } record R {}";
        var file = AvroFile.Parse(new SourceText("test.avdl", text), Options, TestContext.Current.CancellationToken);
        Assert.True(file.IsValid, string.Join("; ", file.Diagnostics));
        Assert.Equal(
            new[] { "record R { ns.Missing a; ns.Missing b; }", "record R {}" },
            file.DeclarationSpans.Select(span => span.ToString()));
        Assert.Equal(text.LastIndexOf("record", StringComparison.Ordinal), file.DeclarationSpans[1].Offset);
        var uses = file.ReferenceSpans[new SchemaName("Missing", "ns")];
        Assert.Equal(new[] { "ns.Missing", "ns.Missing" }, uses.Select(span => span.ToString()));
        Assert.True(uses[0].Offset < uses[1].Offset);
        Assert.Equal(text.IndexOf("ns.R", StringComparison.Ordinal), Assert.Single(file.ReferenceSpans[new SchemaName("R", "ns")]).Offset);
    }

    [Fact]
    public void Duplicate_diagnostic_points_to_later_occurrence()
    {
        const string text = "schema R; record R {} record R {}";
        var compilation = Bind(("test.avdl", text));
        var diagnostic = Assert.Single(compilation.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.DuplicateSchema, diagnostic.Code);
        Assert.Equal(text.LastIndexOf("record", StringComparison.Ordinal), diagnostic.SourceSpan.Offset);
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
        const string text = "schema R; record R { Z first; A second; Z third; }";
        var diagnostic = Assert.Single(Bind(("test.avdl", text)).Diagnostics);
        Assert.Equal(AvroDiagnosticCode.MissingReferences, diagnostic.Code);
        Assert.Equal(text.IndexOf('Z'), diagnostic.SourceSpan.Offset);
        Assert.Equal("Z", diagnostic.SourceSpan.ToString());
        Assert.Contains("A, Z", diagnostic.GetMessage());
    }

    [Theory]
    [InlineData("import idl \"missing.avdl\"; schema R; record R {}", "\"missing.avdl\"")]
    [InlineData("protocol P { import schema \"wrong.avdl\"; }", "\"wrong.avdl\"")]
    public void Import_failures_point_to_the_literal(string text, string literal)
    {
        var diagnostic = Assert.Single(Bind(("test.avdl", text)).Diagnostics);
        Assert.Contains(diagnostic.Code, new[] { AvroDiagnosticCode.MissingImport, AvroDiagnosticCode.InvalidImportFileExtension });
        Assert.Equal(literal, diagnostic.SourceSpan.ToString());
        Assert.Equal(text.IndexOf(literal, StringComparison.Ordinal), diagnostic.SourceSpan.Offset);
    }

    [Fact]
    public void Cycle_points_to_the_closing_import_edge()
    {
        var diagnostic = Assert.Single(
            Bind(
                ("a.avdl", "import idl \"b.avdl\"; schema A; record A {}"),
                ("b.avdl", "import idl \"a.avdl\"; schema B; record B {}")).Diagnostics);
        Assert.Equal("b.avdl", diagnostic.SourceSpan.SourceText.Path.OriginalPath);
        Assert.Equal("\"a.avdl\"", diagnostic.SourceSpan.ToString());
        Assert.Contains("a.avdl -> b.avdl -> a.avdl", diagnostic.GetMessage());
    }

    [Theory]
    [InlineData("schema F; fixed F(0);", "0")]
    [InlineData("protocol P { string call() oneway; }", "oneway")]
    public void Semantic_validation_returns_a_precise_diagnostic(string text, string anchor)
    {
        var result = AvdlParser.ParseFile(new SourceText("test.avdl", text), Options, TestContext.Current.CancellationToken);
        Assert.False(result.IsValid);
        Assert.Equal(anchor, Assert.Single(result.Diagnostics).SourceSpan.ToString());
    }

    [Fact]
    public void Syntax_validation_returns_diagnostics()
    {
        var result = AvdlParser.ParseFile(new SourceText("test.avdl", "schema ;"), Options, TestContext.Current.CancellationToken);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Diagnostics);
    }

    [Theory]
    [InlineData("test.avsc", "{\"type\":\"record\",\"name\":\"R\",\"fields\":null}")]
    [InlineData("test.avpr", "{\"protocol\":\"P\",\"types\":null,\"messages\":{}}")]
    public void Json_validation_returns_precise_value_diagnostics(string path, string text)
    {
        var source = new SourceText(path, text);
        var result = AvscParser.ParseFile(source, Options, TestContext.Current.CancellationToken);
        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.InvalidArrayProperty, diagnostic.Code);
        var offset = text.IndexOf("null", StringComparison.Ordinal);
        Assert.Equal(source.GetSourceSpan(offset, "null".Length), diagnostic.SourceSpan);
        Assert.Contains("must be an array", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    private static AvroCompilation Bind(params (string Path, string Text)[] sources) =>
        SchemaCompilerTestHelpers.Bind(ReferenceResolution.Deferred, DuplicateResolution.Error, sources);
}
