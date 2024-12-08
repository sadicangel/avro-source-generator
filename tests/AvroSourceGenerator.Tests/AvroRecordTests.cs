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
    public Task Generates_Correctly_For(string version) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;

        namespace AvroSourceGenerator.Tests;

        [Avro(LanguageFeatures = LanguageFeatures.{{version}})]
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
        .UseParameters(version);

    [Theory]
    [InlineData("class")]
    [InlineData("record")]
    public Task Generates_Correct_Declaration(string declaration) => TestHelper.Verify($$""""
        using System;
        using AvroSourceGenerator;
        
        namespace AvroSourceGenerator.Tests;
        
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
        
        namespace AvroSourceGenerator.Tests;
        
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
}
