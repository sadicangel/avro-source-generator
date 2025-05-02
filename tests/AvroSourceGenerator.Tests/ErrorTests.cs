using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public sealed class ErrorTests
{
    [Theory]
    [InlineData("null"), InlineData("{}")]
    public Task Verify_Fields_Diagnostic(string fields) => TestHelper.VerifyDiagnostic($$""""
    {
        "type": "error",
        "namespace": "SchemaNamespace",
        "name": "Error",
        "fields": {{fields}}
    }
    """");
}
