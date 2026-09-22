using System.Collections.Immutable;
using AvroSourceGenerator.Avsc;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Templating;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Tests.Avsc;

public sealed class AvscSchemaParserLocationTests
{
    [Theory]
    [InlineData(GenerationTarget.Apache, false)]
    [InlineData(GenerationTarget.Chr, true)]
    [InlineData(GenerationTarget.Legacy, false)]
    [InlineData(GenerationTarget.Modern, true)]
    public void Matches_existing_semantics_for_representative_schemas(GenerationTarget target, bool nullableReferences)
    {
        const string text = """
            {
              "type":"record",
              "name":"Envelope",
              "namespace":"Example",
              "doc":"documentation",
              "aliases":["OldEnvelope"],
              "fields":[
                {"name":"id","type":{"type":"string","logicalType":"uuid"},"default":"00000000-0000-0000-0000-000000000000"},
                {"name":"values","type":{"type":"array","items":{"type":"map","values":"long"}}},
                {"name":"choice","type":["null",{"type":"record","name":"Nested","fields":[]}]}
              ],
              "custom":{"x":[1,true,null]},
              "custom":{"x":[2,false,null]}
            }
            """;
        var source = new SourceText("test.avsc", text);
        var options = new AvroParseOptions(target, nullableReferences);

        var actual = AvscSchemaParser.Parse(source, options, TestContext.Current.CancellationToken);

        Assert.True(actual.IsValid, string.Join("; ", actual.Diagnostics));
        var actualSchemas = actual.Declarations.ToDictionary(static schema => schema.SchemaName);
        Assert.Equal(actual.Declarations.Length, actualSchemas.Count);
        Assert.All(actual.Declarations, schema => Assert.NotEmpty(schema.ToJsonString(actualSchemas)));
        Assert.Equal("{\"x\":[2,false,null]}", actual.RootSchema!.Properties["custom"].GetRawText());
        var actualRecord = Assert.IsType<RecordSchema>(actual.RootSchema);
        Assert.Equal("\"00000000-0000-0000-0000-000000000000\"", actualRecord.Fields[0].DefaultJson?.GetRawText());
    }

    [Fact]
    public void Reports_required_array_failures_at_precise_spans_in_parser_order()
    {
        const string text = """{"protocol":"P","types":0,"messages":{"a":{"request":false,"response":"Missing"},"b":{"request":{},"response":"null"}}}""";
        var source = new SourceText("test.avpr", text);

        var file = AvscSchemaParser.Parse(source, Options, TestContext.Current.CancellationToken);

        Assert.False(file.IsValid);
        Assert.NotNull(file.RootSchema);
        Assert.Equal(2, file.Diagnostics.Length);
        Assert.Collection(
            file.Diagnostics,
            diagnostic => AssertDiagnostic(
                diagnostic,
                source,
                "0",
                "Property 'types' must be an array, but '0' was found."),
            diagnostic => AssertDiagnostic(
                diagnostic,
                source,
                "false",
                "Property 'request' must be an array, but 'False' was found."));
    }

    [Theory]
    [InlineData("{\"type\":\"record\",\"name\":\"R\"}", "fields")]
    [InlineData("{\"protocol\":\"P\",\"messages\":{}}", "types")]
    [InlineData("{\"protocol\":\"P\",\"types\":[],\"messages\":{\"m\":{\"response\":\"null\"}}}", "request")]
    public void Reports_missing_required_arrays_at_the_containing_object(string text, string propertyName)
    {
        var source = new SourceText("test.avpr", text);

        var file = AvscSchemaParser.Parse(source, Options, TestContext.Current.CancellationToken);

        var diagnostic = Assert.Single(file.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.MissingSchemaProperty, diagnostic.Code);
        Assert.Equal($"Required property '{propertyName}' is missing.", diagnostic.GetMessage());
        Assert.StartsWith("{", diagnostic.SourceSpan.ToString(), StringComparison.Ordinal);
        Assert.EndsWith("}", diagnostic.SourceSpan.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Invalid_recovered_files_do_not_bind_or_render_or_add_missing_reference_diagnostics()
    {
        const string text = """{"protocol":"P","types":0,"messages":{"m":{"request":false,"response":"Missing"}}}""";
        var file = AvscSchemaParser.Parse(new SourceText("test.avpr", text), Options, TestContext.Current.CancellationToken);
        var files = ImmutableArray.Create(file);
        var symbols = SymbolTable.FromFiles(files, TestContext.Current.CancellationToken);
        var bound = BoundAvroFile.Bind(
            LinkedAvroFile.Link(file, symbols, TestContext.Current.CancellationToken),
            TestContext.Current.CancellationToken);
        var compilation = AvroCompilation.Create(
            [bound],
            new AvroCompilationOptions(ReferenceResolution.Deferred, DuplicateResolution.Error),
            TestContext.Current.CancellationToken);
        var renderable = RenderableAvroFile.Create(
            bound,
            compilation,
            new RenderOptions(GenerationTarget.Modern, LanguageFeatures.Latest, AccessModifier.Public),
            TestContext.Current.CancellationToken);

        Assert.Null(bound.RootSchema);
        Assert.Empty(bound.Declarations);
        Assert.DoesNotContain(compilation.Diagnostics, diagnostic => diagnostic.Code == AvroDiagnosticCode.MissingReferences);
        Assert.Empty(renderable.EmittedSchemas);
        Assert.Empty(AvroTemplate.Render(renderable, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Records_precise_declaration_and_reference_spans()
    {
        const string text = """{"type":"record","name":"R","fields":[{"name":"value","type":"Other"}]}""";
        var source = new SourceText("test.avsc", text);

        var file = AvscSchemaParser.Parse(source, Options, TestContext.Current.CancellationToken);

        Assert.Equal(text, Assert.Single(file.DeclarationSpans).ToString());
        Assert.Equal("\"Other\"", Assert.Single(file.ReferenceSpans[new SchemaName("Other")]).ToString());
    }

    [Fact]
    public void Recoverable_array_validation_uses_the_selected_duplicate_property()
    {
        const string text = """{"type":"record","name":"R","fields":[],"fields":false}""";
        var source = new SourceText("test.avsc", text);

        var file = AvscSchemaParser.Parse(source, Options, TestContext.Current.CancellationToken);

        var diagnostic = Assert.Single(file.Diagnostics);
        var offset = text.LastIndexOf("false", StringComparison.Ordinal);
        Assert.Equal(source.GetSourceSpan(offset, "false".Length), diagnostic.SourceSpan);
        Assert.Equal(
            "Property 'fields' must be an array, but 'False' was found.",
            diagnostic.GetMessage());
    }

    [Fact]
    public void Propagates_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            AvscSchemaParser.Parse(new SourceText("test.avsc", "{}"), Options, cancellation.Token));
    }

    private static AvroParseOptions Options { get; } = new AvroParseOptions(GenerationTarget.Modern, true);

    private static void AssertDiagnostic(AvroDiagnostic diagnostic, SourceText source, string value, string message)
    {
        var offset = source.Text.IndexOf(value, StringComparison.Ordinal);
        Assert.Equal(AvroDiagnosticCode.InvalidArrayProperty, diagnostic.Code);
        Assert.Equal(source.GetSourceSpan(offset, value.Length), diagnostic.SourceSpan);
        Assert.Equal(message, diagnostic.GetMessage());
    }
}
