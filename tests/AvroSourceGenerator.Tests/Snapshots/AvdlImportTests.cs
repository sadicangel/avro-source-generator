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

        Assert.Contains(output.Diagnostics, diagnostic => diagnostic.Id == "AVROSG1000");
        Assert.Empty(output.Documents);
    }
}
