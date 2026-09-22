using AvroSourceGenerator.Avdl.Syntax;
using AvroSourceGenerator.Avsc.Syntax;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Tests.Avsc;

public sealed class ParserPipelineTests
{
    [Fact]
    public void Qualified_names_ignore_namespace_properties_and_nested_names_inherit()
    {
        const string text = """{"type":"record","name":"Example.R","namespace":false,"fields":[{"name":"e","type":{"type":"enum","name":"E","symbols":["class"]}},{"name":"r","type":"Example.R"}]}""";
        var file = AvscParser.Parse(new SourceText("test.avsc", text), Options, TestContext.Current.CancellationToken);
        Assert.True(file.IsValid);
        var record = Assert.IsType<RecordSchema>(file.RootSchema);
        Assert.Equal(new SchemaName("R", "Example"), record.SchemaName);
        var enumeration = Assert.IsType<EnumSchema>(record.Fields[0].Type);
        Assert.Equal(new SchemaName("E", "Example"), enumeration.SchemaName);
        Assert.Equal("@class", Assert.Single(enumeration.Symbols));
        Assert.Empty(file.References);
    }

    [Fact]
    public void Avdl_exposes_syntax_and_file_parsing_with_provenance()
    {
        var source = new SourceText("test.avdl", "namespace Example; schema R; record R { string value; }");
        var syntax = AvdlParser.Parse(source, TestContext.Current.CancellationToken);
        Assert.Empty(syntax.Diagnostics);
        Assert.Single(syntax.Document.Declarations);
        var file = AvdlParser.ParseFile(source, Options, TestContext.Current.CancellationToken);
        Assert.True(file.IsValid);
        var record = Assert.IsType<RecordSchema>(Assert.Single(file.Declarations));
        Assert.Equal(new SchemaName("R", "Example"), record.SchemaName);
        Assert.Equal("value", Assert.Single(record.Fields).Name.SchemaName);
        Assert.Equal("record R { string value; }", Assert.Single(file.DeclarationSpans).ToString());
    }

    private static AvroParseOptions Options => new AvroParseOptions(GenerationTarget.Modern, true);
}
