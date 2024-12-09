using System.Text.Json;

namespace AvroSourceGenerator.Tests;

public class AvroEnumTests
{
    [Theory]
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


    [Theory]
    [InlineData("false")]
    [InlineData("true")]
    public Task Generates_Correct_Namespace(string useCSharpNamespace) => TestHelper.Verify($$""""
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Single line comment")]
    [InlineData("Multi\nline\ncomment")]
    public Task Generates_Correct_Summary(string? doc) => TestHelper.Verify($$""""
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
                        "type": "enum", "name": "TestEnum", "doc": {{(doc is null ? "null" : $"\"{JsonEncodedText.Encode(doc)}\"")}}, "symbols": []
                    } }
                ]
            }
            """;
        }
        """")
        .UseParameters(doc);

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("[\"Alias1\"]")]
    [InlineData("[\"Alias1\", \"Alias2\"]")]
    public Task Generates_Correct_Aliases(string aliases) => TestHelper.Verify($$""""
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
    [InlineData("public")]
    [InlineData("string")]
    [InlineData("foreach")]
    public Task Generates_Correct_Name_For_Reserved_Keywords(string name) => TestHelper.Verify($$""""
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
                        "type": "enum", "name": "{{name}}", "symbols": []
                    } }
                ]
            }
            """;
        }
        """")
        .UseParameters(name);

    [Fact]
    public Task Generates_Diagnostic_For_Missing_Name() => TestHelper.Verify(""""
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
                        "type": "enum", "symbols": []
                    } }
                ]
            }
            """;
        }
        """");

    [Theory]
    [InlineData("null")]
    [InlineData("\"\"")]
    [InlineData("[]")]
    public Task Generates_Diagnostic_For_Invalid_Name(string name) => TestHelper.Verify($$""""
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
    [InlineData("null.name1.name2")]
    [InlineData("name1.null.name2")]
    [InlineData("name1.name2.null")]
    public Task Generates_Correct_Namespace_For_Reserved_Keywords(string @namespace) => TestHelper.Verify($$""""
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
                        "type": "enum", "name": "TestEnum", "namespace": "{{@namespace}}", "symbols": []
                    } }
                ]
            }
            """;
        }
        """")
        .UseParameters(@namespace);

    [Theory]
    [InlineData("\"\"")]
    [InlineData("[]")]
    public Task Generates_Diagnostic_For_Invalid_Namespace(string @namespace) => TestHelper.Verify($$""""
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
}
