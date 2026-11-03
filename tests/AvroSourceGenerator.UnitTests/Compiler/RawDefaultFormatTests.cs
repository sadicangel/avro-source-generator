using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Schemas.Defaults;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.UnitTests.Compiler;

public sealed class RawDefaultFormatTests
{
    [Theory]
    [InlineData("schema")]
    [InlineData("protocol")]
    [InlineData("idl-field")]
    [InlineData("idl-parameter")]
    public void Field_and_parameter_defaults_remain_raw_in_every_format(string format)
    {
        const string Field = """{"name":"Value","type":"bytes","default":"\u0100"}""";
        var (path, text) = format switch
        {
            "schema" => ("input.avsc", $$"""{"type":"record","name":"R","fields":[{{Field}}]}"""),
            "protocol" => ("input.avpr", "{\"protocol\":\"R\",\"types\":[],\"messages\":{\"M\":{\"request\":[" + Field + "],\"response\":\"null\"}}}"),
            "idl-field" => ("input.avdl", "schema R; record R { bytes Value = \"\\u0100\"; }"),
            "idl-parameter" => ("input.avdl", "protocol R { void M(bytes Value = \"\\u0100\"); }"),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
        var project = AvroCompiler.Compile([new SourceText(path, text)], new AvroParseOptions(GenerationTarget.Modern, LanguageFeatures.CSharp12),
            new AvroCompilationOptions(), TestContext.Current.CancellationToken);
        Assert.True(project.IsValid);
        Assert.Empty(project.Diagnostics);
        var schema = project.Schemas[new SchemaName("R")];
        var value = schema switch
        {
            RecordSchema record => Assert.Single(record.Fields).Default,
            ProtocolSchema protocol => Assert.Single(Assert.Single(protocol.Messages).RequestParameters).Default,
            _ => throw new InvalidOperationException()
        };
        Assert.Equal("\"\\u0100\"", Assert.IsType<UnboundDefaultValue>(value).Json.GetRawText());
        Assert.Contains("\"default\":\"\\u0100\"", schema.ToJsonString(project.Schemas));
    }
}
