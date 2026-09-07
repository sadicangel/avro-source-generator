namespace AvroSourceGenerator.Tests.Snapshots;

public sealed class AvdlImportTests
{
    private const string TopLevelImportSource = """
        import idl "common.avdl";
        schema string;
        """;

    private const string ProtocolImportSource = """
        protocol Service {
            import idl "common.avdl";
            void ping();
        }
        """;

    private const string DependencySource = """
        namespace Example;
        schema Common;

        record Common {
            string value;
        }
        """;

    private const string ConsumerSource = """
        namespace Example;
        import idl "common.avdl";
        schema Consumer;

        record Consumer {
            Common common;
        }
        """;

    private const string ProtocolConsumerSource = """
        namespace Example;

        protocol Service {
            import idl "common.avdl";
            Common getCommon();
        }
        """;

    [Fact]
    public Task Diagnostic_TopLevelImport() => Snapshot.Diagnostic(
        ProjectFile.Source(TopLevelImportSource));

    [Fact]
    public Task Diagnostic_ProtocolImport() => Snapshot.Diagnostic(
        ProjectFile.Source(ProtocolImportSource));

    [Fact]
    public void Diagnostic_ProtocolImport_DoesNotGenerateDocuments()
    {
        var output = GeneratorOutput.Create(
            GeneratorInput.Create([ProjectFile.Source(ProtocolImportSource)], Snapshot.References, Snapshot.ProjectConfig));

        Assert.Contains(output.Diagnostics, diagnostic => diagnostic.Id == "AVROSG1001");
        Assert.Empty(output.Documents);
    }

    [Fact]
    public void Deferred_TopLevelImport_ResolvesDependencyFromAdditionalFiles()
    {
        var output = GenerateDeferred(
            ("consumer.avdl", ConsumerSource),
            ("common.avdl", DependencySource));

        Assert.Empty(output.Diagnostics);
        Assert.Contains(output.Documents, document => document.Content.Contains("global::Example.Common common", StringComparison.Ordinal));
    }

    [Fact]
    public void Deferred_ProtocolImport_ResolvesDependencyFromAdditionalFiles()
    {
        var output = GenerateDeferred(
            ("consumer.avdl", ProtocolConsumerSource),
            ("common.avdl", DependencySource));

        Assert.Empty(output.Diagnostics);
        Assert.Contains(output.Documents, document => document.Content.Contains("global::Example.Common", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("idl")]
    [InlineData("schema")]
    [InlineData("protocol")]
    public void Deferred_ImportKinds_ValidatePaths(string importKind)
    {
        var source = $$"""
            import {{importKind}} "missing-and-unused.file";
            schema Standalone;

            record Standalone { }
            """;

        var output = GenerateDeferred(("consumer.avdl", source));

        Assert.Contains(output.Diagnostics, diagnostic => diagnostic.Id == "AVROSG1001");
        Assert.Empty(output.Documents);
    }

    [Fact]
    public void Deferred_MissingReferencedDependency_ReportsDiagnosticAndDoesNotGenerateDocuments()
    {
        var output = GenerateDeferred(("consumer.avdl", ConsumerSource));

        Assert.Contains(output.Diagnostics, diagnostic => diagnostic.Id == "AVROSG1001");
        Assert.Empty(output.Documents);
    }

    private static GeneratorOutput GenerateDeferred(params (string Path, string Source)[] sources) =>
        GeneratorOutput.Create(
            GeneratorInput.Create(
                [.. sources.Select(static source => ProjectFile.Source(source.Source, source.Path))],
                Snapshot.References,
                Snapshot.ProjectConfig with { ReferenceResolution = "Deferred" }));
}
