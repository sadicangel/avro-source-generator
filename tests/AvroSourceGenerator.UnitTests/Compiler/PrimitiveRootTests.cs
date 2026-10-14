using System.Text.Json;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.UnitTests.Compiler;

public sealed class PrimitiveRootTests
{
    private static readonly AvroParseOptions s_options = new AvroParseOptions(GenerationTarget.Modern, LanguageFeatures.NullableReferenceTypes);

    [Theory]
    [InlineData("key.avsc", "\"null\"", SchemaType.Null)]
    [InlineData("key.avsc", "\"boolean\"", SchemaType.Boolean)]
    [InlineData("key.avsc", "\"int\"", SchemaType.Int)]
    [InlineData("key.avsc", "\"long\"", SchemaType.Long)]
    [InlineData("key.avsc", "\"float\"", SchemaType.Float)]
    [InlineData("key.avsc", "\"double\"", SchemaType.Double)]
    [InlineData("key.avsc", "\"bytes\"", SchemaType.Bytes)]
    [InlineData("key.avsc", "\"string\"", SchemaType.String)]
    [InlineData("key.avsc", """{"type":"string","doc":"Kafka key","custom":42}""", SchemaType.String)]
    [InlineData("key.avsc", """{"type":"array","items":"string"}""", SchemaType.Array)]
    [InlineData("key.avsc", """{"type":"map","values":"long"}""", SchemaType.Map)]
    [InlineData("key.avsc", """["null","string"]""", SchemaType.Union)]
    [InlineData("key.avsc", """{"type":"int","logicalType":"date"}""", SchemaType.Logical)]
    [InlineData("key.avsc", """{"type":"string","logicalType":"uuid"}""", SchemaType.Logical)]
    [InlineData("key.avdl", "schema string;", SchemaType.String)]
    [InlineData("key.avdl", "schema array<string>;", SchemaType.Array)]
    [InlineData("key.avdl", "schema map<long>;", SchemaType.Map)]
    [InlineData("key.avdl", "schema union { null, string };", SchemaType.Union)]
    [InlineData("key.avdl", "schema date;", SchemaType.Logical)]
    public void Core_parses_and_compiles_primitive_roots(string path, string text, SchemaType expected)
    {
        var source = new SourceText(path, text);
        var token = TestContext.Current.CancellationToken;
        var file = AvroFile.Parse(source, s_options, token);
        Assert.True(file.IsValid);
        Assert.Empty(file.Diagnostics);
        Assert.Same(source, file.Text);
        Assert.Equal(expected, file.RootSchema.SchemaType);
        Assert.Empty(file.Declarations);
        Assert.Empty(file.References);
        Assert.Empty(file.Imports);
        Assert.Empty(file.Dependencies);
        var json = file.RootSchema.ToJsonString(new Dictionary<SchemaName, TopLevelSchema>());
        using var document = JsonDocument.Parse(json);
        var roundTrip = AvroFile.Parse(new SourceText("roundtrip.avsc", json), s_options, token);
        Assert.True(roundTrip.IsValid);
        Assert.Equal(expected, roundTrip.RootSchema.SchemaType);

        var compilation = AvroCompiler.Compile([source], s_options, cancellationToken: token);
        Assert.True(compilation.IsValid);
        Assert.Empty(compilation.Diagnostics);
        Assert.Empty(compilation.Schemas);
        Assert.Equal(expected, Assert.Single(compilation.Files).RootSchema!.SchemaType);
    }

    [Theory]
    [InlineData("key.avsc", "  \"string\" ", "  \"string\" ")]
    [InlineData("key.avsc", """{"type":"array","items":"long"}""", """{"type":"array","items":"long"}""")]
    [InlineData("key.avsc", """{"type":"int","logicalType":"date"}""", """{"type":"int","logicalType":"date"}""")]
    [InlineData("key.avdl", "schema string;", "string")]
    [InlineData("key.avdl", "schema array<string>;", "array<string>")]
    public void Named_root_policy_preserves_diagnostic_provenance(string path, string text, string span)
    {
        var source = new SourceText(path, text);
        var options = s_options with { RequireTopLevelSchema = true };
        var file = AvroFile.Parse(source, options, TestContext.Current.CancellationToken);
        Assert.False(file.IsValid);
        var diagnostic = Assert.Single(file.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.MissingRootSchema, diagnostic.Code);
        Assert.Equal("Named schema or protocol required", diagnostic.GetMessage());
        Assert.Same(source, diagnostic.SourceSpan.SourceText);
        Assert.Equal(span, diagnostic.SourceSpan.ToString());
        Assert.Empty(file.Declarations);
    }

    [Fact]
    public void Named_root_policy_keeps_the_idl_import_exemption()
    {
        var file = AvroFile.Parse(
            new SourceText("key.avdl", "import schema \"shared.avsc\"; schema string;"),
            s_options with { RequireTopLevelSchema = true },
            TestContext.Current.CancellationToken);
        Assert.True(file.IsValid);
        Assert.Single(file.Imports);
        Assert.Equal(SchemaType.String, file.RootSchema.SchemaType);
    }

    [Fact]
    public void Named_root_policy_participates_in_cache_equality()
    {
        var source = new SourceText("key.avsc", "\"string\"");
        var strict = s_options with { RequireTopLevelSchema = true };
        var permissiveFile = AvroFile.Parse(source, s_options, TestContext.Current.CancellationToken);
        var strictFile = AvroFile.Parse(source, strict, TestContext.Current.CancellationToken);
        Assert.NotEqual(s_options, strict);
        Assert.NotEqual(s_options.GetHashCode(), strict.GetHashCode());
        Assert.NotEqual(permissiveFile, strictFile);
        Assert.NotEqual(permissiveFile.GetHashCode(), strictFile.GetHashCode());
        var repeated = AvroFile.Parse(source, s_options, TestContext.Current.CancellationToken);
        Assert.Equal(permissiveFile, repeated);
        Assert.Equal(permissiveFile.GetHashCode(), repeated.GetHashCode());
    }

    [Fact]
    public void Parser_machinery_is_not_exported()
    {
        Assert.DoesNotContain(typeof(AvroFile).Assembly.GetExportedTypes(), type =>
            type.Namespace is "AvroSourceGenerator.Avdl" or "AvroSourceGenerator.Avjs" ||
            type.Namespace?.StartsWith("AvroSourceGenerator.Avdl.", StringComparison.Ordinal) == true ||
            type.Name.EndsWith("Parser", StringComparison.Ordinal));
    }
}
