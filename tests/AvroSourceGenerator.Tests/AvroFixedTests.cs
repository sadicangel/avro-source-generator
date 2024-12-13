namespace AvroSourceGenerator.Tests;

public class AvroFixedTests
{
    [Theory]
    [InlineData("public"), InlineData("internal"), InlineData("protected internal"), InlineData("private"), InlineData("private protected"), InlineData("file"), InlineData("")]
    public Task Verify_AccessModifier(string accessModifier) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace CSharpNamespace;
        
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
    [InlineData("\"Fixed\""), InlineData("\"fixed_name\"")]
    [InlineData("\"public\""), InlineData("\"string\"")]
    [InlineData("null"), InlineData("\"\""), InlineData("[]")]
    public Task Verify_Name(string name) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace CSharpNamespace;
        
        [Avro(AvroSchema)]
        partial class Fixed
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
    [InlineData("null"), InlineData("\"FixedSchemaNamespace\"")]
    [InlineData("\"fixed\""), InlineData("\"name1.fixed.name2\"")]
    [InlineData("\"\""), InlineData("[]")]
    public Task Verify_Namespace(string @namespace) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace CSharpNamespace;
        
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
        
        namespace CSharpNamespace;
        
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
        
        namespace CSharpNamespace;
        
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
        
        namespace CSharpNamespace;
        
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
        
        namespace CSharpNamespace;
        
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
    [InlineData("false")]
    [InlineData("true")]
    public Task Verify_UseCSharpNamespace(string useCSharpNamespace) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace CSharpNamespace;
        
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
        .UseParameters(useCSharpNamespace);
}
