using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Compiler.Configuration;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Cases = AvroSourceGenerator.UnitTests.Compiler.SemanticValidationTests;

namespace AvroSourceGenerator.UnitTests.Apache;

public sealed class SemanticValidationTests
{
    [Fact]
    public void Empty_and_unset_namespaces_inherit_the_enclosing_namespace_in_apache_schemas()
    {
        const string Text = """
            {"type":"record","name":"Parent","namespace":"example","fields":[
              {"name":"Empty","type":{"type":"record","name":"EmptyChild","namespace":"","fields":[]}},
              {"name":"Null","type":{"type":"record","name":"NullChild","namespace":null,"fields":[]}},
              {"name":"Omitted","type":{"type":"record","name":"OmittedChild","fields":[]}}
            ]}
            """;
        var token = TestContext.Current.CancellationToken;
        var compiled = AvroCompiler.Compile([new SourceText("namespace.avsc", Text)],
            new AvroParseOptions(GenerationTarget.Apache, LanguageFeatures.CSharp12), cancellationToken: token);
        Assert.True(compiled.IsValid, string.Join(Environment.NewLine, compiled.Diagnostics));
        var root = Assert.Single(compiled.Files).RootSchema!;
        var record = Assert.IsType<Avro.RecordSchema>(Avro.Schema.Parse(root.ToJsonString(compiled.Schemas)));
        Assert.Equal(["example.EmptyChild", "example.NullChild", "example.OmittedChild"],
            record.Fields.Select(field => Assert.IsType<Avro.RecordSchema>(field.Schema).Fullname));
    }

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
