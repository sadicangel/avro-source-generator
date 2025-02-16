namespace AvroSourceGenerator.Tests;

public sealed class AvroErrorTests
{
    [Theory]
    [InlineData("public"), InlineData("internal"), InlineData("protected internal"), InlineData("private"), InlineData("private protected"), InlineData("file"), InlineData("")]
    public Task Verify_AccessModifier(string accessModifier) => TestHelper.Verify("""
        {
            "type": "error",
            "namespace": "SchemaNamespace",
            "name": "Error",
            "fields": []
        }
        """, $$""""
        using System;
        using AvroSourceGenerator;
        
        namespace SchemaNamespace;
        
        [Avro]
        {{accessModifier}} partial class Error;
        """")
        .UseParameters(accessModifier);

    [Theory]
    [InlineData("null"), InlineData("\"\""), InlineData("\"Single line comment\""), InlineData("\"Multi\\nline\\ncomment\"")]
    [InlineData("1"), InlineData("[]"), InlineData("{}")]
    public Task Verify_Documentation(string doc) => TestHelper.Verify($$""""
        {
            "type": "error",
            "namespace": "SchemaNamespace",
            "name": "Error",
            "doc": {{doc}},
            "fields": []
        }
        """")
        .UseParameters(doc);

    [Theory]
    [InlineData("null"), InlineData("[]"), InlineData("[\"Alias1\"]"), InlineData("[\"Alias1\", \"Alias2\"]")]
    [InlineData("\"not an array\""), InlineData("{}")]
    public Task Verify_Aliases(string aliases) => TestHelper.Verify($$""""
        {
            "type": "error",
            "namespace": "SchemaNamespace",
            "name": "Error",
            "aliases": {{aliases}},
            "fields": []
        }
        """")
        .UseParameters(aliases);

    [Theory]
    [InlineData("null"), InlineData("\"not an array\""), InlineData("{}")]
    public Task Verify_Fields(string fields) => TestHelper.Verify($$""""
        {
            "type": "error",
            "namespace": "SchemaNamespace",
            "name": "Error",
            "fields": {{fields}}
        }
        """")
        .UseParameters(fields);

    [Theory]
    [MemberData(nameof(TestData.GetLanguageVersions), MemberType = typeof(TestData))]
    public Task Verify_LanguageFeatures(string languageFeatures) => TestHelper.Verify("""
        {
            "type": "error",
            "namespace": "SchemaNamespace",
            "name": "Error",
            "fields": []
        }
        """, $$""""
        using System;
        using AvroSourceGenerator;
        
        namespace SchemaNamespace;
        
        [Avro(LanguageFeatures = LanguageFeatures.{{languageFeatures}})]
        public partial class Error;
        """")
        .UseParameters(languageFeatures);
}
