using System.Text;
using AvroSourceGenerator.Cli;
using AvroSourceGenerator.Compiler;
using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.UnitTests;

public sealed class CliTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "avro-cli-tests", Guid.NewGuid().ToString("N"));

    private string Output => Path.Combine(_directory, "output");

    [Theory]
    [InlineData("--library", "Auto")]
    [InlineData("--library", "2147483647")]
    [InlineData("--library", "unknown")]
    [InlineData("--library", "Apache,None")]
    [InlineData("--access-modifier", "1")]
    [InlineData("--language-features", "Records,64")]
    [InlineData("--preview-features", "Unions,invalid")]
    [InlineData("--reference-resolution", "Strict,Deferred")]
    [InlineData("--duplicate-resolution", "unknown")]
    [InlineData("--record-declaration", "struct")]
    [InlineData("--language-version", "16")]
    public async Task Invalid_options_do_not_write(string option, string value)
    {
        var input = Write("schema.avsc", Schema("Example"));
        var (code, _, error) = await Run("compile", input, "-o", Output, option, value);
        Assert.Equal(2, code);
        Assert.Contains(value, error);
        Assert.False(Directory.Exists(Output));
    }

    [Theory]
    [InlineData("Chr")]
    [InlineData("chr")]
    public async Task Removed_runtime_reports_migration_advice_without_writing(string library)
    {
        var input = Write("schema.avsc", Schema("Example"));
        var (code, _, error) = await Run("compile", input, "-o", Output, "--library", library);

        Assert.Equal(2, code);
        Assert.Contains("support was removed", error);
        Assert.Contains("Apache", error);
        Assert.Contains("None", error);
        Assert.Contains("0.7.x", error);
        Assert.False(Directory.Exists(Output));
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("compile", "--help")]
    [InlineData("--version")]
    public async Task Help_and_version_do_not_require_inputs(params string[] args)
    {
        var (code, output, error) = await Run(args);
        Assert.Equal(0, code);
        Assert.NotEmpty(output);
        Assert.Empty(error);
        Assert.False(Directory.Exists(Output));
    }

    [Fact]
    public async Task Force_and_check_are_mutually_exclusive()
    {
        var input = Write("schema.avsc", Schema("Example"));
        var (code, _, error) = await Run("compile", input, "-o", Output, "--force", "--check");
        Assert.Equal(2, code);
        Assert.Contains("cannot be combined", error);
        Assert.False(Directory.Exists(Output));
    }

    [Theory]
    [InlineData("--library")]
    [InlineData("--language-features")]
    [InlineData("--preview-features")]
    public async Task Missing_option_values_are_usage_errors(string option)
    {
        var input = Write("schema.avsc", Schema("Example"));
        Assert.Equal(2, (await Run("compile", input, "-o", Output, option)).Code);
        Assert.False(Directory.Exists(Output));
    }

    [Theory]
    [InlineData("--force=invalid")]
    [InlineData("--check=invalid")]
    public async Task Invalid_boolean_values_are_usage_errors(string option)
    {
        var input = Write("schema.avsc", Schema("Example"));
        Assert.Equal(2, (await Run("compile", input, "-o", Output, option)).Code);
        Assert.False(Directory.Exists(Output));
    }

    [Fact]
    public async Task Default_output_matches_explicit_defaults()
    {
        var input = Write("schema.avsc", Schema("Example"));
        Assert.Equal(0, (await Run("compile", input, "-o", Output)).Code);
        var expected = File.ReadAllText(Path.Combine(Output, "Example.Avro.g.cs"));
        var (code, _, error) = await Run(
            "compile",
            input,
            "-o",
            Output,
            "--force",
            "--library",
            "none",
            "--language-version",
            "14",
            "--language-features",
            "All",
            "--preview-features",
            "none",
            "--access-modifier",
            "public",
            "--record-declaration",
            "record",
            "--reference-resolution",
            "strict",
            "--duplicate-resolution",
            "error");
        Assert.Equal(0, code);
        Assert.Empty(error);
        Assert.Equal(expected, File.ReadAllText(Path.Combine(Output, "Example.Avro.g.cs")));
    }

    [Fact]
    public async Task Existing_files_are_preserved_and_other_outputs_continue()
    {
        var first = Write("first.avsc", Schema("First"));
        var second = Write("second.avsc", Schema("Second"));
        var existing = Write("output/First.Avro.g.cs", "user content");
        var (code, _, error) = await Run("compile", first, second, "-o", Output);
        Assert.Equal(1, code);
        Assert.Contains("First.Avro.g.cs", error);
        Assert.Equal("user content", File.ReadAllText(existing));
        Assert.True(File.Exists(Path.Combine(Output, "Second.Avro.g.cs")));
        Assert.Equal(0, (await Run("compile", first, second, "-o", Output, "-f")).Code);
        Assert.NotEqual("user content", File.ReadAllText(existing));
        // Even identical existing output is a collision without explicit force.
        Assert.Equal(1, (await Run("compile", first, second, "-o", Output)).Code);
    }

    [Fact]
    public async Task Force_skips_identical_files_and_preserves_stale_files()
    {
        var input = Write("schema.avsc", Schema("Example"));
        Assert.Equal(0, (await Run("compile", input, "-o", Output)).Code);
        var generated = Path.Combine(Output, "Example.Avro.g.cs");
        var timestamp = DateTime.UtcNow.AddDays(-1);
        File.SetLastWriteTimeUtc(generated, timestamp);
        timestamp = File.GetLastWriteTimeUtc(generated);
        var extra = Write("output/Stale.Avro.g.cs", "stale");
        Assert.Equal(0, (await Run("compile", input, "-o", Output, "--force")).Code);
        Assert.Equal(timestamp, File.GetLastWriteTimeUtc(generated));
        Assert.Equal("stale", File.ReadAllText(extra));
        Assert.Equal(new byte[] { 0xef, 0xbb, 0xbf }, File.ReadAllBytes(generated).Take(3));
    }

    [Fact]
    public async Task Check_is_read_only_and_ignores_bom_newlines_and_extra_files()
    {
        var input = Write("schema.avsc", Schema("Example"));
        Assert.Equal(1, (await Run("compile", input, "-o", Output, "--check")).Code);
        Assert.False(Directory.Exists(Output));
        Assert.Equal(0, (await Run("compile", input, "-o", Output)).Code);
        var generated = Path.Combine(Output, "Example.Avro.g.cs");
        File.WriteAllText(generated, File.ReadAllText(generated).ReplaceLineEndings("\n"), new UTF8Encoding(false));
        Write("output/extra.txt", "unrelated");
        var before = File.ReadAllBytes(generated);
        Assert.Equal(0, (await Run("compile", input, "-o", Output, "--check")).Code);
        Assert.Equal(before, File.ReadAllBytes(generated));
        File.WriteAllText(generated, "changed");
        Assert.Equal(1, (await Run("compile", input, "-o", Output, "--check")).Code);
        Assert.Equal("changed", File.ReadAllText(generated));
    }

    [Fact]
    public async Task Files_directories_and_globs_resolve_imports_and_deduplicate()
    {
        var child = Write("schemas/nested/child.avsc", Schema("Child"));
        var parent = Write("schemas/parent.avdl", "protocol Parent { import schema \"nested/child.avsc\"; record Envelope { Child Value; } }");
        var inputs = new[] { Path.Combine(_directory, "schemas"), Path.Combine(_directory, "schemas/**/*.avsc"), parent, child };
        Assert.Equal(2, InputDiscovery.Discover(inputs, TestContext.Current.CancellationToken).Length);
        var (code, _, error) = await Run(["compile", .. inputs, "-o", Output]);
        Assert.Equal(0, code);
        Assert.Empty(error);
        Assert.True(File.Exists(Path.Combine(Output, "Child.Avro.g.cs")));
        Assert.True(File.Exists(Path.Combine(Output, "Envelope.Avro.g.cs")));
    }

    [Fact]
    public async Task Imports_do_not_read_unselected_files()
    {
        Write("child.avsc", Schema("Child"));
        var parent = Write("parent.avdl", "protocol Parent { import schema \"child.avsc\"; record Envelope { Child Value; } }");
        var (code, _, error) = await Run("compile", parent, "-o", Output);
        Assert.Equal(1, code);
        Assert.Contains("AVROSG5002", error);
        Assert.False(File.Exists(Path.Combine(Output, "Child.Avro.g.cs")));
    }

    [Fact]
    public async Task Input_order_controls_ignored_duplicates()
    {
        var first = Write("a.avsc", Schema("Example", "First"));
        var second = Write("b.avsc", Schema("Example", "Second"));
        var (code, _, _) = await Run("compile", second, first, "-o", Output, "--duplicate-resolution", "Ignore");
        Assert.Equal(0, code);
        var text = File.ReadAllText(Path.Combine(Output, "Example.Avro.g.cs"));
        Assert.Contains("Second", text);
        Assert.DoesNotContain("First", text);
        Assert.Equal(new[] { first, second }, InputDiscovery.Discover([Path.Combine(_directory, "*.avsc")], TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("missing.avsc")]
    [InlineData("missing/**/*.avsc")]
    [InlineData("unsupported.cs")]
    public async Task Invalid_inputs_fail_before_writing(string input)
    {
        if (input.EndsWith(".cs", StringComparison.Ordinal)) Write(input, "class Example {}");
        var (code, _, error) = await Run("compile", Path.Combine(_directory, input), "-o", Output);
        Assert.Equal(2, code);
        Assert.NotEmpty(error);
        Assert.False(Directory.Exists(Output));
    }

    [Fact]
    public async Task Filesystem_failure_is_reported_without_a_compiler_diagnostic()
    {
        var input = Write("schema.avsc", Schema("Example"));
        var destination = Write("destination", "file, not a directory");
        var (code, _, error) = await Run("compile", input, "-o", destination);
        Assert.Equal(1, code);
        Assert.Contains(destination, error);
        Assert.DoesNotContain("AVROSG", error);
    }

    [Fact]
    public async Task Cancellation_returns_130_without_writing()
    {
        var input = Write("schema.avsc", Schema("Example"));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await using var output = new StringWriter();
        await using var error = new StringWriter();
        Assert.Equal(130, await CliApplication.RunAsync(["compile", input, "-o", Output], output, error, cancellation.Token));
        Assert.DoesNotContain("AVROSG", error.ToString());
        Assert.False(Directory.Exists(Output));
    }

    public static IEnumerable<object[]> ParityCases()
    {
        foreach (var library in new[] { "None", "Apache" })
            foreach (var version in new[] { "7.3", "8", "9", "10", "11", "12", "13", "14", "15", "preview" })
                yield return [library, version, "All", "None", "record"];
        foreach (var features in Enum.GetNames<LanguageFeatures>())
            yield return ["None", "14", features, "None", "record"];
        yield return ["Apache", "preview", "All", "Unions", "record"];
        yield return ["None", "preview", "CSharp15", "Unions", "class"];
        yield return ["Apache", "12", "NullableReferenceTypes,InitOnlyProperties", "None", "class"];
    }

    [Theory]
    [MemberData(nameof(ParityCases))]
    public async Task Generated_sources_match_generator(string library, string version, string features, string preview, string declaration)
    {
        var content = "{\"type\":\"record\",\"name\":\"Example\",\"fields\":[{\"name\":\"Value\",\"type\":[\"null\",\"string\",\"int\"],\"default\":null}]}";
        var input = Write("schema.avsc", content);
        var language = version == "preview" ? LanguageVersion.Preview : (LanguageVersion)(version == "7.3" ? 703 : int.Parse(version) * 100);
        var config = new ProjectConfig(language)
        {
            AvroLibrary = library,
            LanguageFeatures = features,
            PreviewFeatures = preview,
            RecordDeclaration = declaration,
            AccessModifier = "Internal",
            ReferenceResolution = "Deferred",
            DuplicateResolution = "Ignore",
        };
        var generatorInput = GeneratorInput.Create([ProjectFile.Schema(content, input)], [], config);
        var generator = generatorInput.GeneratorDriver.RunGenerators(generatorInput.Compilation, TestContext.Current.CancellationToken).GetRunResult().Results.Single();
        var (code, _, error) = await Run(
            "compile",
            input,
            "-o",
            Output,
            "--library",
            library,
            "--language-version",
            version,
            "--language-features",
            features,
            "--preview-features",
            preview,
            "--record-declaration",
            declaration,
            "--access-modifier",
            "internal",
            "--reference-resolution",
            "Deferred",
            "--duplicate-resolution",
            "Ignore");
        Assert.Equal(0, code);
        Assert.Empty(error);
        Assert.NotEmpty(generator.GeneratedSources);
        Assert.Equal(generator.GeneratedSources.Length, Directory.GetFiles(Output).Length);
        foreach (var source in generator.GeneratedSources)
            Assert.Equal(source.SourceText.ToString(), File.ReadAllText(Path.Combine(Output, source.HintName)));
    }

    [Fact]
    public async Task Diagnostics_match_generator_and_independent_files_generate()
    {
        var invalid = Write("invalid.avsc", "{\n \"type\": \"record\", \"name\": \"Broken\", \"fields\": false\n}");
        var valid = Write("valid.avsc", Schema("Valid"));
        var dependent = Write("dependent.avdl", "protocol Dependent { import schema \"invalid.avsc\"; record Wrapper { Broken Value; } }");
        var generatorInput = GeneratorInput.Create(
            [
                ProjectFile.Schema(File.ReadAllText(invalid), invalid), ProjectFile.Schema(File.ReadAllText(valid), valid),
                ProjectFile.Source(File.ReadAllText(dependent), dependent),
            ],
            [],
            new ProjectConfig((LanguageVersion)1400) { AvroLibrary = "None" });
        var generator = generatorInput.GeneratorDriver.RunGenerators(generatorInput.Compilation, TestContext.Current.CancellationToken).GetRunResult().Results.Single();
        var (code, _, error) = await Run("compile", invalid, valid, dependent, "-o", Output);
        Assert.Equal(1, code);
        var lines = error.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(generator.Diagnostics.Length, lines.Length);
        for (var index = 0; index < lines.Length; index++)
        {
            var diagnostic = generator.Diagnostics[index];
            var span = diagnostic.Location.GetLineSpan();
            Assert.Equal($"{span.Path}({span.StartLinePosition.Line + 1},{span.StartLinePosition.Character + 1}): {diagnostic.Severity.ToString().ToLowerInvariant()} {diagnostic.Id}: {diagnostic.GetMessage()}", lines[index]);
        }
        Assert.True(File.Exists(Path.Combine(Output, "Valid.Avro.g.cs")));
        Assert.False(File.Exists(Path.Combine(Output, "Wrapper.Avro.g.cs")));
    }

    private string Write(string relative, string content)
    {
        var path = Path.Combine(_directory, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    private static string Schema(string name, string field = "Value") =>
        $"{{\"type\":\"record\",\"name\":\"{name}\",\"fields\":[{{\"name\":\"{field}\",\"type\":\"string\"}}]}}";

    private static async Task<(int Code, string Output, string Error)> Run(params string[] args)
    {
        await using var output = new StringWriter();
        await using var error = new StringWriter();
        var code = await CliApplication.RunAsync(args, output, error, TestContext.Current.CancellationToken);
        return (code, output.ToString(), error.ToString());
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }
}
