using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public sealed class FixedTests
{
    [Theory]
    [InlineData("16")]
    public Task Verify_Size(string size) => TestHelper.VerifySourceCode($$"""
    {
        "type": "fixed",
        "namespace": "SchemaNamespace",
        "name": "Fixed",
        "size": {{size}}
    }
    """);

    [Theory]
    [InlineData("0"), InlineData("-1"), InlineData("1.1"), InlineData("null"), InlineData("\"A\"")]
    public Task Verify_Size_Diagnostic(string size) => TestHelper.VerifyDiagnostic($$"""
        {
            "type": "fixed",
            "namespace": "SchemaNamespace",
            "name": "Fixed",
            "size": {{size}}
        }
        """);
}
