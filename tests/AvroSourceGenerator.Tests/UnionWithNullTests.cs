using AvroSourceGenerator.Tests.Helpers;

namespace AvroSourceGenerator.Tests;

public sealed class UnionWithNullTests
{
    [Fact]
    public Task Verify()
    {
        var schema = """
            {
              "type": "record",
              "name": "UserProfile",
              "namespace": "com.example.user",
              "doc": "User profile showing various types of union fields",
              "fields": [
                {
                  "name": "emptyUnion",
                  "type": [],
                  "doc": "Invalid in Avro: Demonstrates an empty union (included for test purposes only)"
                },
                {
                  "name": "preferences",
                  "type": [
                    {
                      "type": "record",
                      "name": "Preferences",
                      "fields": [
                        { "name": "language", "type": "string" },
                        { "name": "darkMode", "type": "boolean", "default": false }
                      ]
                    }
                  ],
                  "default": {
                    "language": "en",
                    "darkMode": false
                  },
                  "doc": "User display and language preferences"
                },
                {
                  "name": "address",
                  "type": [
                    "null",
                    {
                      "type": "record",
                      "name": "Address",
                      "fields": [
                        { "name": "street", "type": "string" },
                        { "name": "city", "type": "string" },
                        { "name": "postalCode", "type": "string" }
                      ]
                    }
                  ],
                  "default": null,
                  "doc": "Mailing address; may be null if not provided"
                },
                {
                  "name": "metadata",
                  "type": [
                    {
                      "type": "record",
                      "name": "ProfileMetadata",
                      "fields": [
                        { "name": "createdAt", "type": "string" },
                        { "name": "lastLogin", "type": ["null", "string"], "default": null }
                      ]
                    },
                    "null"
                  ],
                  "default": {
                    "createdAt": "1970-01-01T00:00:00Z",
                    "lastLogin": null
                  },
                  "doc": "Additional profile info, optional field"
                },
                {
                  "name": "contact",
                  "type": [
                    "null",
                    {
                      "type": "record",
                      "name": "EmailContact",
                      "fields": [
                        { "name": "email", "type": "string" }
                      ]
                    },
                    {
                      "type": "record",
                      "name": "PhoneContact",
                      "fields": [
                        { "name": "phoneNumber", "type": "string" }
                      ]
                    }
                  ],
                  "default": null,
                  "doc": "User can be contacted by email or phone; field is optional"
                }
              ]
            }
            
            """;

        return TestHelper.VerifySourceCode(schema);
    }
}
