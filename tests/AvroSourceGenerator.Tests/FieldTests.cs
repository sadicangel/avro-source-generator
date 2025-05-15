using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public class FieldTests
{
    public static MatrixTheoryData<string, string> Primitives => new(
        ["record", "error"],
        ["null", "boolean", "int", "long", "float", "double", "bytes", "string"]);

    [Theory]
    [MemberData(nameof(Primitives))]
    public Task Verify_Primitive(string @class, string field)
    {
        var schema = $$"""
        {
            "type": "{{@class}}",
            "name": "Class",
            "namespace": "SchemaNamespace",
            "fields": [
                {
                    "type": "{{field}}",
                    "name": "Field"
                }
            ]
        }
        """;
        return TestHelper.VerifySourceCode(schema);
    }

    [Theory]
    [InlineData("record"), InlineData("error")]
    public Task Verify_Enum(string @class)
    {
        var schema = $$"""
        {
            "type": "{{@class}}",
            "name": "Class",
            "namespace": "SchemaNamespace",
            "fields": [
                {
                    "type": {
                        "type": "enum",
                        "name": "Enum",
                        "symbols": ["A", "B", "C"]
                    },
                    "name": "Field"
                }
            ]
        }
        """;
        return TestHelper.VerifySourceCode(schema);
    }

    [Theory]
    [InlineData("record"), InlineData("error")]
    public Task Verify_Enum_Nullable(string @class)
    {
        var schema = $$"""
        {
            "type": "{{@class}}",
            "name": "Class",
            "namespace": "SchemaNamespace",
            "fields": [
                {
                    "type": {
                        "type": "enum",
                        "name": "Enum",
                        "symbols": ["A", "B", "C"]
                    },
                    "name": "Field"
                },
                {
                    "type": ["null", "Enum"],
                    "name": "NullableField"
                }
            ]
        }
        """;
        return TestHelper.VerifySourceCode(schema);
    }

    [Theory]
    [MemberData(nameof(Primitives))]
    public Task Verify_Primitive_Nullable(string @class, string field)
    {
        var schema = $$"""
        {
            "type": "{{@class}}",
            "name": "Class",
            "namespace": "SchemaNamespace",
            "fields": [
                {
                    "type": ["null", "{{field}}"],
                    "name": "Field"
                }
            ]
        }
        """;
        return TestHelper.VerifySourceCode(schema);
    }

    [Theory]
    [InlineData("record"), InlineData("error")]
    public Task Verify_Metadata_Properties(string @class)
    {
        var schema = $$"""
        {
            "type": "{{@class}}",
            "name": "Class",
            "namespace": "SchemaNamespace",
            "fields": [
                {
                    "type": ["null", "string"],
                    "name": "Field",
                    "tags": ["Tag1", "Tag2"],
                    "parent": {
                        "name": "object1"                        
                    }
                }
            ]
        }
        """;
        return TestHelper.VerifySourceCode(schema);
    }

    public static TheoryData<string, string> Defaults => new([
        ("null", "null"),
        ("boolean", "true"),
        ("int", "42"),
        ("long", "42"),
        ("float", "42.0"),
        ("double", "42.0"),
        ("bytes", @"""\u0034\u0032"""),
        ("string", @"""FortyTwo"""),
        ]);

    [Theory]
    [MemberData(nameof(Defaults))]
    public Task Verify_Default(string type, string value)
    {
        var schema = $$"""
        {
            "type": "record",
            "name": "Record",
            "namespace": "SchemaNamespace",
            "fields": [
                {
                    "type": "{{type}}",
                    "name": "Field",
                    "default": {{value}}
                }
            ]
        }
        """;
        return TestHelper.VerifySourceCode(schema);
    }
}
