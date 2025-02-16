namespace AvroSourceGenerator.Tests;

public class AvroFixedTests
{
    [Theory]
    [InlineData("public"), InlineData("internal"), InlineData("protected internal"), InlineData("private"), InlineData("private protected"), InlineData("file"), InlineData("")]
    public Task Verify_AccessModifier(string accessModifier) => TestHelper.Verify("""
        {
            "type": "fixed",
            "namespace": "SchemaNamespace",
            "name": "Fixed",
            "size": 16
        }
        """, $$"""
        using System;
        using AvroSourceGenerator;
        
        namespace SchemaNamespace;
        
        [Avro]
        {{accessModifier}} partial class Fixed;
        """)
        .UseParameters(accessModifier);

    [Theory]
    [InlineData("null"), InlineData("\"\""), InlineData("\"Single line comment\""), InlineData("\"Multi\\nline\\ncomment\"")]
    [InlineData("1"), InlineData("[]"), InlineData("{}")]
    public Task Verify_Documentation(string doc) => TestHelper.Verify($$""""
        {
            "type": "fixed",
            "namespace": "SchemaNamespace",
            "name": "Fixed",
            "doc": {{doc}},
            "size": 16
        }
        """")
        .UseParameters(doc);

    [Theory]
    [InlineData("null"), InlineData("[]"), InlineData("[\"Alias1\"]"), InlineData("[\"Alias1\", \"Alias2\"]")]
    [InlineData("\"not an array\""), InlineData("{}")]
    public Task Verify_Aliases(string aliases) => TestHelper.Verify($$"""
        {
            "type": "fixed",
            "namespace": "SchemaNamespace",
            "name": "Fixed",
            "aliases": {{aliases}},
            "size": 16
        }
        """)
        .UseParameters(aliases);

    [Theory]
    [InlineData("16")]
    [InlineData("0"), InlineData("-1"), InlineData("null"), InlineData("[]"), InlineData("\"A\"")]
    public Task Verify_Size(string size) => TestHelper.Verify($$"""
        {
            "type": "fixed",
            "namespace": "SchemaNamespace",
            "name": "Fixed",
            "size": {{size}}
        }
        """)
        .UseParameters(size);

    [Theory]
    [MemberData(nameof(TestData.GetLanguageVersions), MemberType = typeof(TestData))]
    public Task Verify_LanguageFeatures(string languageFeatures) => TestHelper.Verify("""
        {
            "type": "fixed",
            "namespace": "SchemaNamespace",
            "name": "Fixed",
            "size": 16
        }
        """, $$""""
        using System;
        using AvroSourceGenerator;
        
        namespace SchemaNamespace;
        
        [Avro(LanguageFeatures = LanguageFeatures.{{languageFeatures}})]
        public partial class Fixed;
        """")
        .UseParameters(languageFeatures);
}
