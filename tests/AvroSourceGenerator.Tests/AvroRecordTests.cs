namespace AvroSourceGenerator.Tests;

public sealed class AvroRecordTests
{
    [Theory]
    [InlineData("public"), InlineData("internal"), InlineData("protected internal"), InlineData("private"), InlineData("private protected"), InlineData("file"), InlineData("")]
    public Task Verify_AccessModifier(string accessModifier) => TestHelper.Verify("""
        {
            "type": "record",
            "namespace": "SchemaNamespace",
            "name": "Record",
            "fields": []
        }
        """, $$"""
        using System;
        using AvroSourceGenerator;
        
        namespace SchemaNamespace;
        
        [Avro]
        {{accessModifier}} partial class Record;
        """)
        .UseParameters(accessModifier);

    [Theory]
    [InlineData("class"), InlineData("record"), InlineData("class record")]
    public Task Verify_Declaration(string declaration) => TestHelper.Verify("""
        {
            "type": "record",
            "namespace": "SchemaNamespace",
            "name": "Record",
            "fields": []
        }
        """, $$"""
        using System;
        using AvroSourceGenerator;
        
        namespace SchemaNamespace;
        
        [Avro]
        partial {{declaration}} Record;
        """)
        .UseParameters(declaration);

    [Theory]
    [InlineData("null"), InlineData("\"\""), InlineData("\"Single line comment\""), InlineData("\"Multi\\nline\\ncomment\"")]
    [InlineData("1"), InlineData("[]"), InlineData("{}")]
    public Task Verify_Documentation(string doc) => TestHelper.Verify($$"""
        {
            "type": "record",
            "namespace": "SchemaNamespace",
            "name": "Record",
            "doc": {{doc}},
            "fields": []
        }
        """)
        .UseParameters(doc);

    [Theory]
    [InlineData("null"), InlineData("[]"), InlineData("[\"Alias1\"]"), InlineData("[\"Alias1\", \"Alias2\"]")]
    [InlineData("\"not an array\""), InlineData("{}")]
    public Task Verify_Aliases(string aliases) => TestHelper.Verify($$"""
        {
            "type": "record",
            "namespace": "SchemaNamespace",
            "name": "Record",
            "aliases": {{aliases}},
            "fields": []
        }
        """)
        .UseParameters(aliases);

    [Theory]
    [InlineData("null"), InlineData("\"not an array\""), InlineData("{}")]
    public Task Verify_Fields(string fields) => TestHelper.Verify($$"""
        {
            "type": "record",
            "namespace": "SchemaNamespace",
            "name": "Record",
            "fields": {{fields}}
        }
        """)
        .UseParameters(fields);

    [Theory]
    [MemberData(nameof(TestData.GetLanguageVersions), MemberType = typeof(TestData))]
    public Task Verify_LanguageFeatures(string languageFeatures) => TestHelper.Verify("""
        {
            "type": "record",
            "namespace": "SchemaNamespace",
            "name": "Record",
            "fields": []
        }
        """, $$"""
        using System;
        using AvroSourceGenerator;
        
        namespace SchemaNamespace;
        
        [Avro(LanguageFeatures = LanguageFeatures.{{languageFeatures}})]
        public partial class Record;
        """)
        .UseParameters(languageFeatures);
}
