using AvroSourceGenerator.Avsc;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Tests.Avsc;

public sealed class InvalidSchemaPropagationTests
{
    [Theory]
    [InlineData("""{"type":"array","items":false}""")]
    [InlineData("""{"type":"map","values":false}""")]
    [InlineData("""["null",false]""")]
    [InlineData("""{"type":"record","name":"R","fields":[{"name":"f","type":false}]}""")]
    [InlineData("""{"type":"error","name":"E","fields":[{"name":"f","type":false}]}""")]
    [InlineData("""{"protocol":"P","types":[],"messages":{"m":{"request":[{"name":"p","type":false}],"response":"null"}}}""")]
    [InlineData("""{"protocol":"P","types":[],"messages":{"m":{"request":[],"response":false}}}""")]
    [InlineData("""{"protocol":"P","types":[],"messages":{"m":{"request":[],"response":"null","errors":[false]}}}""")]
    public void Invalid_child_stops_parent_construction(string text)
    {
        var source = new SourceText("test.avsc", text);
        var file = AvscSchemaParser.Parse(source, Options, TestContext.Current.CancellationToken);

        Assert.Same(AvroSchema.Null, file.RootSchema);
        Assert.False(file.IsValid);
        Assert.Empty(file.Declarations);
        var diagnostic = Assert.Single(file.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.InvalidSchema, diagnostic.Code);
        Assert.Equal("false", diagnostic.SourceSpan.ToString());
        Assert.Equal(AvroDiagnostic.InvalidSchema(diagnostic.SourceSpan, "Invalid schema: false").GetMessage(), diagnostic.GetMessage());
    }

    [Theory]
    [InlineData("""{"type":"array"}""")]
    [InlineData("""{"type":"fixed","name":"F","size":0}""")]
    [InlineData("""{"type":"enum","name":"E","symbols":[null]}""")]
    [InlineData("""{"type":"record","name":false,"fields":[]}""")]
    [InlineData("""{"type":"record","name":"R","fields":[],"aliases":[false]}""")]
    [InlineData("""{"type":"record","name":"R","fields":[],"doc":false}""")]
    [InlineData("""{"type":"record","name":"R","namespace":false,"fields":[]}""")]
    [InlineData("""{"type":"record","name":"R","fields":[],"logicalType":false}""")]
    [InlineData("""{"protocol":"P","types":[],"messages":{"m":false}}""")]
    [InlineData("""{"protocol":"P","types":[],"messages":{"m":{"request":[],"response":"null","one-way":0}}}""")]
    public void Invalid_properties_return_the_invalid_sentinel(string text)
    {
        var file = AvscSchemaParser.Parse(new SourceText("test.avsc", text), Options, TestContext.Current.CancellationToken);

        Assert.Same(AvroSchema.Null, file.RootSchema);
        Assert.Empty(file.Declarations);
        Assert.Equal(AvroDiagnosticCode.InvalidSchema, Assert.Single(file.Diagnostics).Code);
    }

    [Fact]
    public void Earlier_recovered_diagnostics_survive_a_later_invalid_child()
    {
        const string text = """{"protocol":"P","types":false,"messages":{"m":{"request":{},"response":false}}}""";
        var file = AvscSchemaParser.Parse(new SourceText("test.avpr", text), Options, TestContext.Current.CancellationToken);

        Assert.Same(AvroSchema.Null, file.RootSchema);
        Assert.Equal(new[] { "false", "{}", "false" }, file.Diagnostics.Select(diagnostic => diagnostic.SourceSpan.ToString()));
        Assert.All(file.Diagnostics, diagnostic => Assert.Equal(AvroDiagnosticCode.InvalidSchema, diagnostic.Code));
    }

    [Fact]
    public void Recursive_declaration_returns_an_invalid_result()
    {
        const string text = """{"type":"record","name":"R","fields":[{"name":"f","type":{"type":"record","name":"R","fields":[]}}]}""";
        var source = new SourceText("test.avsc", text);
        var file = AvscSchemaParser.Parse(source, Options, TestContext.Current.CancellationToken);

        Assert.Same(AvroSchema.Null, file.RootSchema);
        Assert.Equal(AvroDiagnostic.InvalidSchema(SourceSpan.FromSourceText(source),
            "Recursive schema definition detected for schema 'R'.").GetMessage(), Assert.Single(file.Diagnostics).GetMessage());
    }

    [Fact]
    public void Invariant_failures_are_not_converted_to_diagnostics()
    {
        var source = new SourceText("test.avsc", """{"type":"string","logicalType":"uuid"}""");
        Assert.Throws<InvalidOperationException>(() => AvscSchemaParser.Parse(
            source, new AvroParseOptions((GenerationTarget)(-1), true), TestContext.Current.CancellationToken));
    }

    private static AvroParseOptions Options { get; } = new(GenerationTarget.Modern, true);
}
