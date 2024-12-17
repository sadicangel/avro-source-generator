namespace AvroSourceGenerator.Tests;

public class AvroFixedTests
{
    [Theory]
    [InlineData("public"), InlineData("internal"), InlineData("protected internal"), InlineData("private"), InlineData("private protected"), InlineData("file"), InlineData("")]
    public Task Verify_AccessModifier(string accessModifier) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace SchemaNamespace;
        
        [Avro(AvroSchema)]
        {{accessModifier}} partial class Fixed
        {
            public const string AvroSchema = """
            {
                "type": "fixed",
                "namespace": "SchemaNamespace",
                "name": "Fixed",
                "size": 16
            }
            """;
        }
        """")
        .UseParameters(accessModifier);

    [Theory]
    [InlineData("\"Fixed\"", "Fixed"), InlineData("\"fixed_name\"", "fixed_name")]
    [InlineData("\"public\"", "@public"), InlineData("\"string\"", "@string")]
    [InlineData("null", "Fixed"), InlineData("\"\"", "Fixed"), InlineData("[]", "Fixed")]
    public Task Verify_Name(string name, string matchingClassName) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace SchemaNamespace;
        
        [Avro(AvroSchema)]
        partial class {{matchingClassName}}
        {
            public const string AvroSchema = """
            {
                "type": "fixed",
                "namespace": "SchemaNamespace",
                "name": {{name}},
                "size": 16
            }
            """;
        }
        """")
        .UseParameters(name);

    [Theory]
    [InlineData("null", "CSharpNamespace"), InlineData("\"FixedSchemaNamespace\"", "FixedSchemaNamespace")]
    [InlineData("\"fixed\"", "@fixed"), InlineData("\"name1.fixed.name2\"", "name1.@fixed.name2")]
    [InlineData("\"\"", "FixedSchemaNamespace"), InlineData("[]", "FixedSchemaNamespace")]
    public Task Verify_Namespace(string @namespace, string matchingNamespace) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace {{matchingNamespace}};
        
        [Avro(AvroSchema)]
        partial class Fixed
        {
            public const string AvroSchema = """
            {
                "type": "fixed",
                "namespace": {{@namespace}},
                "name": "Fixed",
                "size": 16
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
        public partial class Fixed
        {        
            public const string AvroSchema = """
            {
                "type": "fixed",
                "namespace": "SchemaNamespace",
                "name": "Fixed",
                "doc": {{doc}},
                "size": 16
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
        public partial class Fixed
        {
            public const string AvroSchema = """
            {
                "type": "fixed",
                "namespace": "SchemaNamespace",
                "name": "Fixed",
                "aliases": {{aliases}},
                "size": 16
            }
            """;
        }
        """")
        .UseParameters(aliases);

    [Theory]
    [InlineData("16")]
    [InlineData("0"), InlineData("-1"), InlineData("null"), InlineData("[]"), InlineData("\"A\"")]
    public Task Verify_Size(string size) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace SchemaNamespace;
        
        [Avro(AvroSchema)]
        public partial class Fixed
        {
            public const string AvroSchema = """
            {
                "type": "fixed",
                "namespace": "SchemaNamespace",
                "name": "Fixed",
                "size": {{size}}
            }
            """;
        }
        """")
        .UseParameters(size);

    [Theory]
    [MemberData(nameof(TestData.GetLanguageVersions), MemberType = typeof(TestData))]
    public Task Verify_LanguageFeatures(string languageFeatures) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace SchemaNamespace;
        
        [Avro(AvroSchema, LanguageFeatures = LanguageFeatures.{{languageFeatures}})]
        public partial class Fixed
        {
            public const string AvroSchema = """
            {
                "type": "fixed",
                "namespace": "SchemaNamespace",
                "name": "Fixed",
                "size": 16
            }
            """;
        }
        """")
        .UseParameters(languageFeatures);

    [Theory]
    [InlineData("false", "SchemaNamespace")]
    [InlineData("false", "CSharpNamespace")]
    [InlineData("true", "CSharpNamespace")]
    public Task Verify_UseCSharpNamespace(string useCSharpNamespace, string csharpNamespace) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace {{csharpNamespace}};
        
        [Avro(AvroSchema, UseCSharpNamespace = {{useCSharpNamespace}})]
        public partial class Fixed
        {
            public const string AvroSchema = """
            {
                "type": "fixed",
                "namespace": "SchemaNamespace",
                "name": "Fixed",
                "size": 16
            }
            """;
        }
        """")
        .UseParameters(useCSharpNamespace, csharpNamespace);
}
