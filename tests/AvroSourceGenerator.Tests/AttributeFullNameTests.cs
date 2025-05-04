using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public sealed class AttributeFullNameTests
{
    [Fact]
    public Task Verify() => TestHelper.VerifySourceCode("""
    {
        "type": "record",
        "name": "SchemaNamespace.Example",
        "fields": []
    }
    """,
    """
    using System;
    using AvroSourceGenerator;
        
    namespace SchemaNamespace;
        
    [Avro]
    internal partial class Example;
    """);

    [Fact]
    public Task Diagnostic() => TestHelper.VerifyDiagnostic("""
    {
        "type": "record",
        "name": "AnotherNamespace.Example",
        "fields": []
    }
    """,
    """
    using System;
    using AvroSourceGenerator;
        
    namespace SchemaNamespace;
        
    [Avro]
    internal partial class Example;
    """);
}
