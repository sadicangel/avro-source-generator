﻿using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public class FieldMetadataPropertyTests
{
    [Theory]
    [InlineData("record"), InlineData("error")]
    public Task Verify(string @class)
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
}
