namespace AvroSourceGenerator.Tests.Chr.Snapshots;

public sealed class SubjectTests
{
    [Fact]
    public Task Verify()
    {
        var @enum = TestSchemas.Get("enum<A,B,C>")
            .With("namespace", "external.namespace");
        var subject = new JsonObject()
            .With(
                "schema",
                TestSchemas.Get("record")
                    .With(
                        "fields",
                        [
                            new
                            {
                                name = "ExternalEnum",
                                type = "external.namespace.Enum"
                            },
                            new
                            {
                                name = "ExternalRecord",
                                type = "external.namespace.Record"
                            }
                        ]).ToJsonString())
            .With("schemaType", "AVRO")
            .With(
                "references",
                [
                    new
                    {
                        name = "external.namespace.Enum",
                        subject = "namespace-Enum",
                        version = 1
                    },
                    new
                    {
                        name = "external.namespace.Record",
                        subject = "namespace-Record",
                        version = 1
                    }
                ]);
        var record = TestSchemas.Get("record")
            .With("namespace", "external.namespace");

        return Snapshot.Files(
        [
            ProjectFile.Schema(@enum.ToJsonString()),
            ProjectFile.Subject(subject.ToJsonString()),
            ProjectFile.Schema(record.ToJsonString())
        ]);
    }

    [Fact]
    public Task Diagnostic_MissingDeclaredReference()
    {
        var subject = CreateSubject(
            [
                new
                {
                    name = "ExternalRecord",
                    type = "external.namespace.Record"
                },
                new
                {
                    name = "ExternalEnum",
                    type = "external.namespace.Enum"
                }
            ],
            [
                new
                {
                    name = "external.namespace.Record",
                    subject = "namespace-Record",
                    version = 1
                },
                new
                {
                    name = "external.namespace.Enum",
                    subject = "namespace-Enum",
                    version = 1
                }
            ]);

        return Snapshot.Diagnostic([ProjectFile.Subject(subject.ToJsonString())]);
    }

    [Fact]
    public Task Diagnostic_ReferenceMustBeDeclared()
    {
        var subject = CreateSubject(
        [
            new
            {
                name = "ExternalRecord",
                type = "external.namespace.Record"
            }
        ]);
        var record = TestSchemas.Get("record")
            .With("namespace", "external.namespace");

        return Snapshot.Diagnostic(
        [
            ProjectFile.Subject(subject.ToJsonString()),
            ProjectFile.Schema(record.ToJsonString())
        ]);
    }

    [Fact]
    public Task Diagnostic_InvalidReferencesShape()
    {
        var subject = CreateSubject()
            .With("references", new JsonObject());

        return Snapshot.Diagnostic([ProjectFile.Subject(subject.ToJsonString())]);
    }

    [Fact]
    public Task Diagnostic_ReferenceMissingName()
    {
        var subject = CreateSubject()
            .With(
                "references",
                [
                    new
                    {
                        subject = "namespace-Record",
                        version = 1
                    }
                ]);

        return Snapshot.Diagnostic([ProjectFile.Subject(subject.ToJsonString())]);
    }

    [Fact]
    public Task Diagnostic_DuplicateTypeAcrossInputs()
    {
        var subject = new JsonObject()
            .With("schema", TestSchemas.Get("record").ToJsonString())
            .With("schemaType", "AVRO");

        return Snapshot.Diagnostic(
        [
            ProjectFile.Subject(subject.ToJsonString()),
            ProjectFile.Schema(TestSchemas.Get("record").ToJsonString())
        ]);
    }

    private static JsonNode CreateSubject(IEnumerable<object?>? fields = null, IEnumerable<object?>? references = null)
    {
        return new JsonObject()
            .With(
                "schema",
                TestSchemas.Get("record")
                    .With("fields", fields ?? [])
                    .ToJsonString())
            .With("schemaType", "AVRO")
            .With("references", references);
    }
}
