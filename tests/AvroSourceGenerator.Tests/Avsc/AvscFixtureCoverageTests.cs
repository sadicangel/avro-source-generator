using System.Collections.Immutable;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Templating;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Tests.Avsc;

public sealed class AvscFixtureCoverageTests
{
    [Theory]
    [InlineData(GenerationTarget.Modern, false)]
    [InlineData(GenerationTarget.Modern, true)]
    [InlineData(GenerationTarget.Legacy, false)]
    [InlineData(GenerationTarget.Legacy, true)]
    [InlineData(GenerationTarget.Apache, false)]
    [InlineData(GenerationTarget.Apache, true)]
    [InlineData(GenerationTarget.Chr, false)]
    [InlineData(GenerationTarget.Chr, true)]
    public void Matches_semantics_binding_and_rendering_for_all_json_fixtures(
        GenerationTarget target,
        bool nullableReferences)
    {
        var sources = GetJsonFixtureSources();
        var options = new AvroParseOptions(target, nullableReferences);
        var files = sources
            .Select(source => AvxxParser.Parse(source, options, TestContext.Current.CancellationToken))
            .ToImmutableArray();

        Assert.All(files, file => Assert.True(file.IsValid, $"{file.Path}: {string.Join("; ", file.Diagnostics)}"));
        Assert.NotEmpty(files.SelectMany(static file => file.Declarations));

        var pipeline = BuildPipeline(files, target, nullableReferences);
        Assert.All(pipeline.BoundFiles, static file => Assert.True(file.IsValid));
        Assert.NotEmpty(pipeline.RenderedSchemas);
    }

    [Fact]
    public void Uses_last_duplicate_message()
    {
        const string text = """
            {
              "protocol":"P",
              "types":[],
              "messages":{
                "m":{"request":[],"response":"null","doc":"first"},
                "m":{"request":[],"response":"null","doc":"second"}
              }
            }
            """;
        var source = new SourceText("test.avpr", text);

        var file = AvxxParser.Parse(source, new AvroParseOptions(GenerationTarget.Modern, true), TestContext.Current.CancellationToken);

        var protocol = Assert.IsType<ProtocolSchema>(file.RootSchema);
        Assert.Equal("second", Assert.Single(protocol.Messages).Documentation);
        Assert.Empty(file.Diagnostics);
    }

    private static PipelineResult BuildPipeline(
        ImmutableArray<AvroFile> files,
        GenerationTarget target,
        bool nullableReferences)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var symbols = SymbolTable.FromFiles(files, cancellationToken);
        var boundFiles = files
            .Select(file => LinkedAvroFile.Link(file, symbols, cancellationToken))
            .Select(file => BoundAvroFile.Bind(file, cancellationToken))
            .ToImmutableArray();
        var compilation = AvroCompilation.Create(
            boundFiles,
            new AvroCompilationOptions(ReferenceResolution.Deferred, DuplicateResolution.Error),
            cancellationToken);
        var languageFeatures = LanguageFeatures.Latest;
        if (!nullableReferences)
            languageFeatures &= ~LanguageFeatures.NullableReferenceTypes;
        var renderOptions = new RenderOptions(target, languageFeatures, AccessModifier.Public);
        var renderableFiles = boundFiles
            .Select(file => RenderableAvroFile.Create(file, compilation, renderOptions, cancellationToken))
            .ToImmutableArray();
        var renderedSchemas = renderableFiles
            .SelectMany(file => AvroTemplate.Render(file, cancellationToken))
            .ToImmutableArray();
        return new PipelineResult(boundFiles, renderableFiles, renderedSchemas);
    }

    private static ImmutableArray<SourceText> GetJsonFixtureSources()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "tests", "Schemas")))
            directory = directory.Parent;

        Assert.NotNull(directory);
        var schemasDirectory = Path.Combine(directory.FullName, "tests", "Schemas");
        return Directory.EnumerateFiles(schemasDirectory)
            .Where(static path => Path.GetExtension(path) is ".avsc" or ".avpr")
            .OrderBy(static path => path, StringComparer.Ordinal)
            .Select(path => new SourceText(Path.GetFileName(path), File.ReadAllText(path)))
            .ToImmutableArray();
    }

    private readonly record struct PipelineResult(
        ImmutableArray<BoundAvroFile> BoundFiles,
        ImmutableArray<RenderableAvroFile> RenderableFiles,
        ImmutableArray<RenderedSchema> RenderedSchemas);
}
