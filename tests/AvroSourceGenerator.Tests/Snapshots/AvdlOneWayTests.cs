namespace AvroSourceGenerator.Tests.Snapshots;

public sealed class AvdlOneWayTests
{
    [Fact]
    public Task Diagnostic_InvalidResponse() => Snapshot.Diagnostic(
        ProjectFile.Source(
            """
            protocol InvalidService {
                string heartbeat() oneway;
            }
            """));
}
