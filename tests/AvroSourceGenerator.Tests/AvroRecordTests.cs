using System.Text.Json;

namespace AvroSourceGenerator.Tests;

public sealed class AvroRecordTests
{
    public static TheoryData<string> GetLanguageFeatures() =>
    [
        nameof(LanguageFeatures.CSharp7_3),
        nameof(LanguageFeatures.CSharp8),
        nameof(LanguageFeatures.CSharp9),
        nameof(LanguageFeatures.CSharp10),
        nameof(LanguageFeatures.CSharp11),
        nameof(LanguageFeatures.CSharp12),
        nameof(LanguageFeatures.CSharp13),
    ];

    [Theory]
    [MemberData(nameof(GetLanguageFeatures))]
    public Task Generates_Correct_Features(string features) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;

        namespace CSharpNamespace;

        [Avro(LanguageFeatures = LanguageFeatures.{{features}})]
        public partial class Record
        {
            public const string AvroSchema = """
            {
                "type": "record",
                "namespace": "SchemaNamespace",
                "name": "Record",
                "fields": [
                    { "name": "StringField", "type": "string" },
                    { "name": "IntField", "type": "int" },
                    { "name": "LongField", "type": "long" },
                    { "name": "FloatField", "type": "float" },
                    { "name": "DoubleField", "type": "double" },
                    { "name": "BooleanField", "type": "boolean" },
                    { "name": "BytesField", "type": "bytes" },
                    { "name": "NullableStringField", "type": ["null", "string"], "default": null },
                    { "name": "DefaultIntField", "type": "int", "default": 42 },
                    { "name": "EnumField", "type": { "type": "enum", "name": "TestEnum", "symbols": ["A", "B", "C"] } },
                    { "name": "ArrayField", "type": { "type": "array", "items": "string" } },
                    { "name": "MapField", "type": { "type": "map", "values": "int" } },
                    { "name": "NestedRecordField", "type": {
                        "type": "record",
                        "name": "NestedRecord",
                        "fields": [
                            { "name": "NestedStringField", "type": "string" },
                            { "name": "NestedIntField", "type": "int" }
                        ]
                    } },
                    { "name": "LogicalDateField", "type": { "type": "int", "logicalType": "date" } },
                    { "name": "LogicalTimestampField", "type": { "type": "long", "logicalType": "timestamp-millis" } }
                ]
            }
            """;
        }
        """")
        .UseParameters(features);

    [Theory]
    [InlineData("class")]
    [InlineData("record")]
    public Task Generates_Correct_Declaration(string declaration) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace CSharpNamespace;
        
        [Avro]
        public partial {{declaration}} Record
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
                "name": "Record",
                "fields": [
                    { "name": "NestedRecordField", "namespace": "SchemaNamespace", "type": {
                        "type": "record",
                        "name": "NestedRecord",
                        "fields": []
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
                "doc": {{(doc is null ? "null" : $"\"{JsonEncodedText.Encode(doc)}\"")}},
                "name": "Record",
                "fields": []
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
                "name": "Record",
                "aliases": {{aliases}},
                "fields": []
            }
            """;
        }
        """")
        .UseParameters(aliases);
}
