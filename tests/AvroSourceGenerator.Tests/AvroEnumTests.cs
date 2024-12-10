namespace AvroSourceGenerator.Tests;

public class AvroEnumTests
{
    [Theory]
    [InlineData("public"), InlineData("internal"), InlineData("protected internal"), InlineData("private"), InlineData("private protected"), InlineData("file"), InlineData("")]
    public Task Verify_AccessModifier(string accessModifier) => TestHelper.Verify($$""""
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

    [Theory]
    [InlineData("\"EnumName\""), InlineData("\"enum_name\"")]
    [InlineData("\"public\""), InlineData("\"string\"")]
    [InlineData("null"), InlineData("\"\""), InlineData("[]")]
    public Task Verify_Name(string name) => TestHelper.Verify($$""""
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
                        "type": "enum", "name": {{name}}, "symbols": []
                    } }
                ]
            }
            """;
        }
        """")
        .UseParameters(name);

    [Theory]
    [InlineData("null"), InlineData("\"EnumSchemaNamespace\"")]
    [InlineData("\"enum\""), InlineData("\"name1.enum.name2\"")]
    [InlineData("\"\""), InlineData("[]")]
    public Task Verify_Namespace(string @namespace) => TestHelper.Verify($$""""
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
                        "type": "enum", "name": "TestEnum", "namespace": {{@namespace}}, "symbols": []
                    } }
                ]
            }
            """;
        }
        """")
        .UseParameters(@namespace);

    [Theory]
    [InlineData("null"), InlineData("\"\""), InlineData("\"Single line comment\""), InlineData("\"Multi\\nline\\ncomment\"")]
    [InlineData("1"), InlineData("[]"), InlineData("{}")]
    public Task Verify_Documentation(string doc) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace CSharpNamespace;
        
        [Avro]
        public partial class Record
        {        
            public const string AvroSchema = """
            {
                "type": "record",
                "namespace": "SchemaNamespace",
                "name": "Wrapper",
                "fields": [
                    { "name": "EnumField", "type": {
                        "type": "enum", "name": "TestEnum", "doc": {{doc}}, "symbols": []
                    } }
                ]
            }
            """;
        }
        """")
        .UseParameters(doc);

    [Theory]
    [InlineData("null"), InlineData("[]"), InlineData("[\"Alias1\"]"), InlineData("[\"Alias1\", \"Alias2\"]")]
    [InlineData("\"not an array\""), InlineData("{}")]
    public Task Verify_Aliases(string aliases) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace CSharpNamespace;
        
        [Avro]
        public partial class Record
        {
            public const string AvroSchema = """
            {
                "type": "record",
                "namespace": "SchemaNamespace",
                "name": "Wrapper",
                "fields": [
                    { "name": "EnumField", "type": {
                        "type": "enum", "name": "TestEnum", "aliases": {{aliases}}, "symbols": []
                    } }
                ]
            }
            """;
        }
        """")
        .UseParameters(aliases);

    [Theory]
    [InlineData("[]"), InlineData("[\"A\"]"), InlineData("[\"B\", \"C\"]")]
    [InlineData("null"), InlineData("\"not an array\""), InlineData("{}")]
    public Task Verify_Symbols(string symbols) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace CSharpNamespace;
        
        [Avro]
        public partial class Record
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
    [InlineData("null"), InlineData("\"A\""), InlineData("\"B\"")]
    [InlineData("\"C\""), InlineData("2"), InlineData("{}")]
    public Task Verify_Default(string @default) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace CSharpNamespace;
        
        [Avro]
        public partial class Record
        {
            public const string AvroSchema = """
            {
                "type": "record",
                "namespace": "SchemaNamespace",
                "name": "Wrapper",
                "fields": [
                    { "name": "EnumField", "type": {
                        "type": "enum", "name": "TestEnum", "symbols": ["A", "B"], "default": {{@default}}
                    } }
                ]
            }
            """;
        }
        """")
        .UseParameters(@default);

    [Theory]
    [MemberData(nameof(TestData.GetLanguageVersions), MemberType = typeof(TestData))]
    public Task Verify_LanguageFeatures(string languageFeatures) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace CSharpNamespace;
        
        [Avro(LanguageFeatures = LanguageFeatures.{{languageFeatures}})]
        public partial class Record
        {
            public const string AvroSchema = """
            {
                "type": "record",
                "namespace": "SchemaNamespace",
                "name": "Wrapper",
                "fields": [
                    { "name": "EnumField", "type": {
                        "type": "enum", "name": "TestEnum", "symbols": ["A", "B"]
                    } }
                ]
            }
            """;
        }
        """")
        .UseParameters(languageFeatures);


    [Theory]
    [InlineData("false")]
    [InlineData("true")]
    public Task Verify_UseCSharpNamespace(string useCSharpNamespace) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace CSharpNamespace;
        
        [Avro(UseCSharpNamespace = {{useCSharpNamespace}})]
        public partial class Record
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
        .UseParameters(useCSharpNamespace);
}
