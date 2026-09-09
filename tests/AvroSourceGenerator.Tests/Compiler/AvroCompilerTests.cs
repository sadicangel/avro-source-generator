using System.Collections.Immutable;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Tests.Compiler;

public sealed class AvroCompilerTests
{
    private static readonly AvroParseOptions ParseOptions = new AvroParseOptions(GenerationTarget.Modern, true);

    [Fact]
    public void Convenience_compiler_matches_independent_stages()
    {
        SourceText[] sources =
        [
            new SourceText(
                "consumer.avdl",
                """
                import schema "shared.avsc";
                schema Consumer;
                record Consumer { Shared value; }
                """),
            new SourceText("shared.avsc", """{"type":"record","name":"Shared","fields":[]}""")
        ];
        var token = TestContext.Current.CancellationToken;
        var options = new AvroCompilationOptions();
        var parsed = sources.Select(source => AvroFile.Parse(source, ParseOptions, token)).ToImmutableArray();
        var symbols = SymbolTable.FromFiles(parsed, token);
        var files = parsed.Select(file => BoundAvroFile.Bind(LinkedAvroFile.Link(file, symbols, token), token)).ToImmutableArray();
        var composed = AvroCompilation.Create(files, options, token);

        var compiled = AvroCompiler.Compile(sources, ParseOptions, options, token);

        Assert.True(compiled.IsValid);
        Assert.Empty(compiled.Diagnostics);
        Assert.Equal(composed, compiled);
        Assert.Equal(composed.GetHashCode(), compiled.GetHashCode());
        Assert.Equal(composed.Files, compiled.Files);
        Assert.Equal(["Consumer", "Shared"], compiled.Schemas.Keys.Select(name => name.FullName).Order());
        Assert.Single(compiled.GetOwnedDeclarations(compiled.Files[0], token));
        Assert.Equal(
            ["consumer.avdl", "shared.avsc"],
            compiled.GetContributingFiles([new SchemaName("Consumer")], token).Select(file => file.File.SourceText.Path));
    }

    [Theory]
    [InlineData("empty.avdl", "", AvroDiagnosticCode.InvalidJson)]
    [InlineData("blank.avsc", " \r\n ", AvroDiagnosticCode.InvalidJson)]
    [InlineData("invalid.avsc", "{", AvroDiagnosticCode.InvalidJson)]
    [InlineData("invalid.avsc", "{}", AvroDiagnosticCode.InvalidSchema)]
    [InlineData("invalid.avdl", "$", AvroDiagnosticCode.InvalidCharacter)]
    public void Invalid_files_return_core_diagnostics(string path, string text, AvroDiagnosticCode expected)
    {
        var project = AvroCompiler.Compile([new SourceText(path, text)], ParseOptions, cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(project.IsValid);
        Assert.False(project.Files[0].File.IsValid);
        Assert.Null(project.Files[0].RootSchema);
        Assert.Contains(project.Diagnostics, diagnostic => diagnostic.Code == expected);
        Assert.All(project.Diagnostics, diagnostic => Assert.Equal(AvroDiagnosticSeverity.Error, diagnostic.Severity));
    }

    [Theory]
    [InlineData(DuplicateResolution.Error, false)]
    [InlineData(DuplicateResolution.Ignore, true)]
    public void Duplicate_policy_preserves_first_owner(DuplicateResolution policy, bool valid)
    {
        const string schema = """{"type":"record","name":"Shared","fields":[]}""";
        var project = AvroCompiler.Compile(
            [new SourceText("first.avsc", schema), new SourceText("second.avsc", schema)],
            ParseOptions,
            new AvroCompilationOptions(ReferenceResolution.Strict, policy),
            TestContext.Current.CancellationToken);
        Assert.Equal(valid, project.IsValid);
        Assert.Single(project.GetOwnedDeclarations(project.Files[0], TestContext.Current.CancellationToken));
        Assert.Empty(project.GetOwnedDeclarations(project.Files[1], TestContext.Current.CancellationToken));
        if (!valid) Assert.Equal(AvroDiagnosticCode.DuplicateSchema, Assert.Single(project.Diagnostics).Code);
    }

    [Fact]
    public void Missing_reference_is_a_core_diagnostic()
    {
        var project = AvroCompiler.Compile([new SourceText("consumer.avdl", "schema Consumer; record Consumer { Missing value; }")], ParseOptions, cancellationToken: TestContext.Current.CancellationToken);
        var diagnostic = Assert.Single(project.Diagnostics);
        Assert.Equal(AvroDiagnosticCode.MissingReferences, diagnostic.Code);
        Assert.Contains("Missing", diagnostic.GetMessage());
    }

    [Fact]
    public void Diamond_imports_deduplicate_contributing_files()
    {
        var project = AvroCompiler.Compile(
            [
                new SourceText("top.avdl", """import idl "left.avdl"; import idl "right.avdl"; schema Top; record Top { Left left; Right right; }"""),
                new SourceText("left.avdl", """import idl "base.avdl"; schema Left; record Left { Base value; }"""),
                new SourceText("right.avdl", """import idl "base.avdl"; schema Right; record Right { Base value; }"""),
                new SourceText("base.avdl", "schema Base; record Base {}")
            ],
            ParseOptions,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(project.IsValid);
        Assert.Equal(
            ["base.avdl", "left.avdl", "right.avdl", "top.avdl"],
            project.GetContributingFiles([new SchemaName("Top")], TestContext.Current.CancellationToken).Select(file => file.File.SourceText.Path));
    }

    [Fact]
    public void Cycles_return_import_diagnostics()
    {
        var project = AvroCompiler.Compile(
            [
                new SourceText("a.avdl", """import idl "b.avdl"; schema A; record A {}"""),
                new SourceText("b.avdl", """import idl "a.avdl"; schema B; record B {}""")
            ],
            ParseOptions,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(project.IsValid);
        Assert.Equal(AvroDiagnosticCode.InvalidImport, Assert.Single(project.Diagnostics).Code);
    }

    [Fact]
    public void Cancellation_propagates_between_files()
    {
        using var cancellation = new CancellationTokenSource();
        Assert.Throws<OperationCanceledException>(() => AvroCompiler.Compile(Sources(), ParseOptions, cancellationToken: cancellation.Token));

        IEnumerable<SourceText> Sources()
        {
            yield return new SourceText("first.avsc", """{"type":"record","name":"First","fields":[]}""");
            cancellation.Cancel();
            yield return new SourceText("second.avsc", "{}");
        }
    }
}
