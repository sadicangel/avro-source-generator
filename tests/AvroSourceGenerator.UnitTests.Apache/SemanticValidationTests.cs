using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Cases = AvroSourceGenerator.UnitTests.Compiler.SemanticValidationTests;

namespace AvroSourceGenerator.UnitTests.Apache;

public sealed class SemanticValidationTests
{
    [Theory]
    [MemberData(nameof(Cases.ValidInputs), MemberType = typeof(Cases))]
    public void Legal_comparison_cases_compile_and_Apache_parses_their_schemas(string extension, string text)
    {
        var token = TestContext.Current.CancellationToken;
        var compiled = AvroCompiler.Compile([new SourceText("valid." + extension, text)], new AvroParseOptions(GenerationTarget.Apache, LanguageFeatures.CSharp12), cancellationToken: token);
        Assert.True(compiled.IsValid, string.Join(Environment.NewLine, compiled.Diagnostics));
        var root = Assert.Single(compiled.Files).RootSchema!;
        var json = root.ToJsonString(compiled.Schemas);
        if (root is ProtocolSchema)
            Assert.NotNull(Avro.Protocol.Parse(json));
        else
            Assert.NotNull(Avro.Schema.Parse(json));

        var input = GeneratorInput.Create([new ProjectFile(text, extension, "valid." + extension)],
            [MetadataReference.CreateFromFile(typeof(Avro.Schema).Assembly.Location)], new ProjectConfig(LanguageVersion.CSharp12) { AvroLibrary = "Apache" });
        var driver = input.GeneratorDriver.RunGeneratorsAndUpdateCompilation(input.Compilation, out var compilation, out var diagnostics, token);
        Assert.Empty(diagnostics);
        Assert.Empty(driver.GetRunResult().Diagnostics);
        using var stream = new MemoryStream();
        var emitted = compilation.Emit(stream, cancellationToken: token);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
    }
}
