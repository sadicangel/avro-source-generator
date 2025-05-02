using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public sealed class RecordTests
{
    [Theory]
    [InlineData("null"), InlineData("{}")]
    public Task Verify_Fields_Diagnostic(string fields) => TestHelper.VerifyDiagnostic($$""""
    {
        "type": "record",
        "namespace": "SchemaNamespace",
        "name": "Record",
        "fields": {{fields}}
    }
    """");
}
