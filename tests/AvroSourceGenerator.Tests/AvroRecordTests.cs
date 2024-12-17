namespace AvroSourceGenerator.Tests;

public sealed class AvroRecordTests
{
    [Theory]
    [InlineData("public"), InlineData("internal"), InlineData("protected internal"), InlineData("private"), InlineData("private protected"), InlineData("file"), InlineData("")]
    public Task Verify_AccessModifier(string accessModifier) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace CSharpNamespace;
        
        [Avro(AvroSchema)]
        {{accessModifier}} partial class Record
        {
            public const string AvroSchema = """
            {
                "type": "record",
                "namespace": "SchemaNamespace",
                "name": "Record",
                "fields": []
            }
            """;
        }
        """")
        .UseParameters(accessModifier);

    [Theory]
    [InlineData("class"), InlineData("record"), InlineData("class record")]
    public Task Verify_Declaration(string declaration) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace CSharpNamespace;
        
        [Avro(AvroSchema)]
        partial {{declaration}} Record
        {
            public const string AvroSchema = """
            {
                "type": "record",
                "namespace": "SchemaNamespace",
                "name": "Record",
                "fields": []
            }
            """;
        }
        """")
        .UseParameters(declaration);

    [Theory]
    [InlineData("\"RecordName\"", "RecordName"), InlineData("\"record_name\"", "record_name")]
    [InlineData("\"class\"", "@class"), InlineData("\"string\"", "@string")]
    [InlineData("null", "Record"), InlineData("\"\"", "Record"), InlineData("[]", "Record")]
    public Task Verify_Name(string name, string matchingClassName) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace CSharpNamespace;
        
        [Avro(AvroSchema)]
        partial class {{matchingClassName}}
        {
            public const string AvroSchema = """
            {
                "type": "record",
                "namespace": "SchemaNamespace",
                "name": {{name}},
                "fields": []
            }
            """;
        }
        """")
        .UseParameters(name);

    [Theory]
    [InlineData("null"), InlineData("\"RecordSchemaNamespace\"")]
    [InlineData("\"class\""), InlineData("\"name1.class.name2\"")]
    [InlineData("\"\""), InlineData("[]")]
    public Task Verify_Namespace(string @namespace) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace CSharpNamespace;
        
        [Avro(AvroSchema)]
        partial class Record
        {
            public const string AvroSchema = """
            {
                "type": "record",
                "namespace": {{@namespace}},
                "name": "Record",
                "fields": []
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
        
        [Avro(AvroSchema)]
        public partial class Record
        {        
            public const string AvroSchema = """
            {
                "type": "record",
                "namespace": "SchemaNamespace",
                "name": "Record",
                "doc": {{doc}},
                "fields": []
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
        
        [Avro(AvroSchema)]
        public partial class Record
        {
            public const string AvroSchema = """
            {
                "type": "record",
                "namespace": "SchemaNamespace",
                "name": "Record",
                "aliases": {{aliases}},
                "fields": []
            }
            """;
        }
        """")
        .UseParameters(aliases);

    [Theory]
    [InlineData("null"), InlineData("\"not an array\""), InlineData("{}")]
    public Task Verify_Fields(string fields) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace CSharpNamespace;
        
        [Avro(AvroSchema)]
        public partial class Record
        {
            public const string AvroSchema = """
            {
                "type": "record",
                "namespace": "SchemaNamespace",
                "name": "Record",
                "fields": {{fields}}
            }
            """;
        }
        """")
        .UseParameters(fields);

    [Theory]
    [MemberData(nameof(TestData.GetLanguageVersions), MemberType = typeof(TestData))]
    public Task Verify_LanguageFeatures(string languageFeatures) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace CSharpNamespace;
        
        [Avro(AvroSchema, LanguageFeatures = LanguageFeatures.{{languageFeatures}})]
        public partial class Record
        {
            public const string AvroSchema = """
            {
                "type": "record",
                "namespace": "SchemaNamespace",
                "name": "Record",
                "fields": []
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
        
        [Avro(AvroSchema, UseCSharpNamespace = {{useCSharpNamespace}})]
        public partial class Record
        {
            public const string AvroSchema = """
            {
                "type": "record",
                "namespace": "SchemaNamespace",
                "name": "Record",
                "fields": []
            }
            """;
        }
        """")
        .UseParameters(useCSharpNamespace);
}
