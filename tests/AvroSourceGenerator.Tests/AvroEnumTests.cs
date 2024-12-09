namespace AvroSourceGenerator.Tests;

public class AvroEnumTests
{
    [Theory]
    [InlineData("null")]
    [InlineData("\"not-an-array\"")]
    [InlineData("[]")]
    [InlineData("[\"A\"]")]
    [InlineData("[\"A\", \"B\"]")]
    public Task Generates_Correct_Enum(string symbols) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace CSharpNamespace;
        
        [Avro]
        partial class Wrapper
        {
            public const string AvroSchema = """
            {
                "type": "record",
                "namespace": "SchemaNamespace",
                "name": "Wrapper",
                "fields": [
                    { "name": "EnumField", "type": {
                        "type": "enum", "name": "TestEnum", "symbols": {{symbols}}
                    } }
                ]
            }
            """;
        }
        """")
        .UseParameters(symbols);

    [Theory]
    [InlineData("public")]
    [InlineData("internal")]
    [InlineData("protected internal")]
    [InlineData("private")]
    [InlineData("private protected")]
    [InlineData("file")]
    [InlineData("")]
    public Task Generates_Correct_AccessModifier(string accessModifier) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace CSharpNamespace;
        
        [Avro]
        {{accessModifier}} partial class Wrapper
        {
            public const string AvroSchema = """
            {
                "type": "record",
                "namespace": "SchemaNamespace",
                "name": "Wrapper",
                "fields": [
                    { "name": "EnumField", "type": {
                        "type": "enum", "name": "TestEnum", "symbols": []
                    } }
                ]
            }
            """;
        }
        """")
        .UseParameters(accessModifier);
}
