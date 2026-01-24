using System.Text.Json;

namespace AvroSourceGenerator.Tests.Data;

public static class TestSchemas
{
    private static readonly Dictionary<string, string> s_schemas = new()
    {
        ["null"] = "\"null\"",
        ["boolean"] = "\"boolean\"",
        ["int"] = "\"int\"",
        ["long"] = "\"long\"",
        ["float"] = "\"float\"",
        ["double"] = "\"double\"",
        ["bytes"] = "\"bytes\"",
        ["string"] = "\"string\"",
        ["enum"] = """
        {
            "type": "enum",
            "name": "Enum",
            "namespace": "SchemaNamespace",
            "symbols": []
        }
        """,
        ["enum<A,B,C>"] = """
        {
            "type": "enum",
            "name": "Enum",
            "namespace": "SchemaNamespace",
            "symbols": ["A", "B", "C"]
        }
        """,
        ["error"] = """
        {
            "type": "error",
            "namespace": "SchemaNamespace",
            "name": "Error",
            "fields": []
        }
        """,
        ["fixed"] = """
        {
            "type": "fixed",
            "namespace": "SchemaNamespace",
            "name": "Fixed",
            "size": 16
        }
        """,
        ["record"] = """
        {
            "type": "record",
            "namespace": "SchemaNamespace",
            "name": "Record",
            "fields": []
        }
        """,
        ["array<string>"] = """
        {
            "type": "array",
            "items": "string"
        }
        """,
        ["array<record>"] = """
        {
            "type": "array",
            "items": {
                "type": "record",
                "namespace": "SchemaNamespace",
                "name": "Record",
                "fields": []
            }
        }
        """,
        ["map<string>"] = """
        {
            "type": "map",
            "values": "string"
        }
        """,
        ["map<record>"] = """
        {
            "type": "map",
            "values": {
                "type": "record",
                "namespace": "SchemaNamespace",
                "name": "Record",
                "fields": []
            }
        }
        """,
        ["[null, string]"] = """
        [
            "null",
            "string"
        ]
        """,
        ["[null, record]"] = """
        [
            "null",
            {
                "type": "record",
                "namespace": "SchemaNamespace",
                "name": "Record",
                "fields": []
            }
        ]
        """,
        ["protocol"] = """
        {
            "protocol": "RpcProtocol",
            "namespace": "SchemaNamespace",
            "types": [],
            "messages": {}
        }
        """
    };

    public static JsonNode Get(string schemaType) => JsonNode.Parse(s_schemas[schemaType])!;

    extension(JsonNode node)
    {
        public JsonNode With(string propertyName, JsonNode propertyValue)
        {
            var clone = node.DeepClone();
            clone[propertyName] = propertyValue;
            return clone;
        }

        public JsonNode With(string propertyName, JsonArray propertyValue) =>
            node.With(propertyName, (JsonNode)propertyValue);

        public JsonNode With(string propertyName, IEnumerable<object?>? propertyValue)
        {
            var clone = node.DeepClone();
            clone[propertyName] = propertyValue is null ? null : new JsonArray([.. propertyValue.Select(Parse)]);
            return clone;

            static JsonNode Parse(object? x) =>
                x is null ? JsonNode.Parse("null")! : JsonNode.Parse(JsonSerializer.Serialize(x, x.GetType()))!;
        }
    }
}
