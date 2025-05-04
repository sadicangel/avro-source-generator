using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public sealed class AttributeRecordDeclarationTests
{
    [Theory]
    [MemberData(nameof(RecordDeclarationSchemaPairs))]
    public Task Verify(string recordDeclaration, string schemaType)
    {
        var schema = TestSchemas.Get(schemaType).ToString();

        var source = s_sources[schemaType].Replace("$recordDeclaration$", recordDeclaration);

        return TestHelper.VerifySourceCode(schema, source);
    }

    public static MatrixTheoryData<string, string> RecordDeclarationSchemaPairs() => new(
        ["record", "class"],
        ["record"]);

    private static readonly Dictionary<string, string> s_sources = new()
    {
        ["record"] = """
        using System;
        using AvroSourceGenerator;

        namespace SchemaNamespace;

        [Avro]
        public partial $recordDeclaration$ Record;
        """
    };
}
