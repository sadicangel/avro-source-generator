using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public class EnumTests
{
    [Theory]
    [InlineData("[]"), InlineData("[\"A\", \"B\"]")]
    public Task Verify_Symbols(string symbols) => TestHelper.VerifySourceCode($$"""
    {
        "type": "enum",
        "name": "TestEnum",
        "namespace": "SchemaNamespace",
        "symbols": {{symbols}}
    }
    """);

    [Theory]
    [InlineData("null"), InlineData("{}")]
    public Task Verify_Symbols_Diagnostic(string symbols) => TestHelper.VerifyDiagnostic($$"""
    {
        "type": "enum",
        "name": "TestEnum",
        "namespace": "SchemaNamespace",
        "symbols": {{symbols}}
    }
    """);
}
