using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.UnitTests.Apache;

public sealed class DefaultPreservationTests
{
    [Theory]
    [InlineData(LanguageVersion.CSharp7_3)]
    [InlineData(LanguageVersion.CSharp12)]
    [InlineData(LanguageVersion.Preview)]
    public void Schema_defaults_round_trip_without_initializing_properties(LanguageVersion version)
    {
        var record = Assert.IsAssignableFrom<Avro.Specific.ISpecificRecord>(DefaultPreservationAssertions.Verify(version, "Apache",
            [MetadataReference.CreateFromFile(typeof(Avro.Schema).Assembly.Location)]));
        using var json = JsonDocument.Parse(record.Schema.ToString());
        var fields = json.RootElement.GetProperty("fields");
        Assert.Equal(7, fields[0].GetProperty("default").GetInt32());
        Assert.Equal("NDI=", fields[2].GetProperty("default").GetString());
        Assert.Equal("[1,2]", fields[3].GetProperty("default").GetRawText());
        Assert.Equal("Second", fields[5].GetProperty("default").GetString());
        Assert.Equal("{}", fields[7].GetProperty("default").GetRawText());
        Assert.Equal("present", fields[8].GetProperty("default").GetString());
    }

    [Fact]
    public void Editing_a_raw_default_refreshes_apache_schema_json_without_adding_an_initializer()
    {
        var input = GeneratorInput.Create([ProjectFile.Schema(DefaultPreservationAssertions.Schema)],
            [MetadataReference.CreateFromFile(typeof(Avro.Schema).Assembly.Location)],
            new ProjectConfig(LanguageVersion.CSharp12) { AvroLibrary = "Apache" });
        var token = TestContext.Current.CancellationToken;
        var driver = input.GeneratorDriver.RunGenerators(input.Compilation, token);
        Assert.Empty(driver.GetRunResult().Diagnostics);
        Assert.Contains("\"default\": 7", string.Join("\n", driver.GetRunResult().GeneratedTrees));
        var original = input.AdditionalTexts.Single();
        var changed = DefaultPreservationAssertions.Schema.Replace("\"default\":7", "\"default\":8");
        driver = driver.ReplaceAdditionalText(original, new EditedText(original.Path, changed)).RunGenerators(input.Compilation, token);
        var result = driver.GetRunResult();
        Assert.Empty(result.Diagnostics);
        var generated = string.Join("\n", result.GeneratedTrees);
        Assert.Contains("\"default\": 8", generated);
        Assert.DoesNotContain(" = 8L;", generated);
        driver = driver.RunGenerators(input.Compilation, token);
        Assert.All(driver.GetRunResult().Results.Single().TrackedOutputSteps.SelectMany(step => step.Value).SelectMany(step => step.Outputs),
            output => Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));
    }

    private sealed class EditedText(string path, string text) : AdditionalText
    {
        public override string Path => path;
        public override Microsoft.CodeAnalysis.Text.SourceText GetText(CancellationToken cancellationToken = default) =>
            Microsoft.CodeAnalysis.Text.SourceText.From(text);
    }
}
