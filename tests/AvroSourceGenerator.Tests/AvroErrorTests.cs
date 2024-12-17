namespace AvroSourceGenerator.Tests;

public sealed class AvroErrorTests
{
    [Theory]
    [InlineData("public"), InlineData("internal"), InlineData("protected internal"), InlineData("private"), InlineData("private protected"), InlineData("file"), InlineData("")]
    public Task Verify_AccessModifier(string accessModifier) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace SchemaNamespace;
        
        [Avro(AvroSchema)]
        {{accessModifier}} partial class Error
        {
            public const string AvroSchema = """
            {
                "type": "error",
                "namespace": "SchemaNamespace",
                "name": "Error",
                "fields": []
            }
            """;
        }
        """")
        .UseParameters(accessModifier);

    [Theory]
    [InlineData("\"ErrorName\"", "ErrorName"), InlineData("\"error_name\"", "error_name")]
    [InlineData("\"class\"", "@class"), InlineData("\"string\"", "@string")]
    [InlineData("null", "Error"), InlineData("\"\"", "Error"), InlineData("[]", "Error")]
    public Task Verify_Name(string name, string matchingClassName) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace SchemaNamespace;
        
        [Avro(AvroSchema)]
        partial class {{matchingClassName}}
        {
            public const string AvroSchema = """
            {
                "type": "error",
                "namespace": "SchemaNamespace",
                "name": {{name}},
                "fields": []
            }
            """;
        }
        """")
        .UseParameters(name);

    [Theory]
    [InlineData("null"), InlineData("\"ErrorSchemaNamespace\"")]
    [InlineData("\"class\""), InlineData("\"name1.class.name2\"")]
    [InlineData("\"\""), InlineData("[]")]
    public Task Verify_Namespace(string @namespace) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace SchemaNamespace;
        
        [Avro(AvroSchema)]
        partial class Error
        {
            public const string AvroSchema = """
            {
                "type": "error",
                "namespace": {{@namespace}},
                "name": "Error",
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
        
        namespace SchemaNamespace;
        
        [Avro(AvroSchema)]
        public partial class Error
        {        
            public const string AvroSchema = """
            {
                "type": "error",
                "namespace": "SchemaNamespace",
                "name": "Error",
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
        
        namespace SchemaNamespace;
        
        [Avro(AvroSchema)]
        public partial class Error
        {
            public const string AvroSchema = """
            {
                "type": "error",
                "namespace": "SchemaNamespace",
                "name": "Error",
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
        
        namespace SchemaNamespace;
        
        [Avro(AvroSchema)]
        public partial class Error
        {
            public const string AvroSchema = """
            {
                "type": "error",
                "namespace": "SchemaNamespace",
                "name": "Error",
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
        
        namespace SchemaNamespace;
        
        [Avro(AvroSchema, LanguageFeatures = LanguageFeatures.{{languageFeatures}})]
        public partial class Error
        {
            public const string AvroSchema = """
            {
                "type": "error",
                "namespace": "SchemaNamespace",
                "name": "Error",
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
        
        namespace SchemaNamespace;
        
        [Avro(AvroSchema, UseCSharpNamespace = {{useCSharpNamespace}})]
        public partial class Error
        {
            public const string AvroSchema = """
            {
                "type": "error",
                "namespace": "SchemaNamespace",
                "name": "Error",
                "fields": []
            }
            """;
        }
        """")
        .UseParameters(useCSharpNamespace);
}
