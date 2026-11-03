using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace AvroSourceGenerator.UnitTests.Infrastructure;

public static class EnumSymbolAssertions
{
    public static string Json(string defaultSymbol) => $$"""
        {"type":"record","name":"R","fields":[{"name":"Value","type":{"type":"enum","name":"E","symbols":["class","normal","int"],"default":"{{defaultSymbol}}"},"default":"{{defaultSymbol}}"}]}
        """;

    public static string Idl(string defaultSymbol) => $$"""
        schema R; enum E { class, normal, `int` } = {{defaultSymbol}}; record R { E Value = "{{defaultSymbol}}"; }
        """;

    public static void Verify(string library, ImmutableArray<MetadataReference> references, string defaultSymbol)
    {
        var outputs = new List<string[]>();
        foreach (var file in new[] { ProjectFile.Schema(Json(defaultSymbol)), ProjectFile.Source(Idl(defaultSymbol)) })
        {
            var input = GeneratorInput.Create(
                [file, ProjectFile.CSharp("public static class Usage { public static R Create() => new R { Value = E.@class }; }")],
                references, new ProjectConfig(LanguageVersion.CSharp12) { AvroLibrary = library });
            var driver = input.GeneratorDriver.RunGeneratorsAndUpdateCompilation(
                input.Compilation, out var compilation, out var diagnostics, TestContext.Current.CancellationToken);
            Assert.Empty(diagnostics);
            Assert.Empty(driver.GetRunResult().Diagnostics);
            using var stream = new MemoryStream();
            var emitted = compilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
            Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
            var enumeration = compilation.GetTypeByMetadataName("E");
            Assert.NotNull(enumeration);
            var members = enumeration.GetMembers().OfType<IFieldSymbol>().Where(member => member.HasConstantValue).ToArray();
            Assert.Equal(["class", "normal", "int"], members.Select(member => member.Name));
            Assert.Equal([0, 1, 2], members.Select(member => member.ConstantValue));
            var sources = driver.GetRunResult().Results.Single().GeneratedSources.OrderBy(source => source.HintName).Select(source => source.SourceText.ToString()).ToArray();
            Assert.Contains(sources, source => source.Contains("@class,"));
            Assert.Contains(sources, source => source.Contains("@int,"));
            outputs.Add(sources);

            driver = driver.RunGenerators(input.Compilation, TestContext.Current.CancellationToken);
            Assert.All(StepTracking.GetTrackedSteps(driver.GetRunResult())["AvroFile"].SelectMany(step => step.Outputs),
                output => Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));
            var changed = new ChangedAdditionalText(input.AdditionalTexts[0].Path, file.Content.Replace("normal", "struct"));
            driver = driver.ReplaceAdditionalText(input.AdditionalTexts[0], changed)
                .RunGeneratorsAndUpdateCompilation(input.Compilation, out compilation, out diagnostics, TestContext.Current.CancellationToken);
            Assert.Empty(diagnostics);
            Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
            Assert.Contains(driver.GetRunResult().Results.Single().GeneratedSources, source => source.SourceText.ToString().Contains("@struct,"));
            Assert.DoesNotContain(driver.GetRunResult().Results.Single().GeneratedSources, source => source.SourceText.ToString().Contains("normal,"));
            driver = driver.ReplaceAdditionalText(changed, input.AdditionalTexts[0]).RunGenerators(input.Compilation, TestContext.Current.CancellationToken);
            Assert.Equal(sources, driver.GetRunResult().Results.Single().GeneratedSources.OrderBy(source => source.HintName).Select(source => source.SourceText.ToString()));
        }
        Assert.Equal(outputs[0], outputs[1]);
    }

    private sealed class ChangedAdditionalText(string path, string content) : AdditionalText
    {
        public override string Path => path;
        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(content);
    }
}
