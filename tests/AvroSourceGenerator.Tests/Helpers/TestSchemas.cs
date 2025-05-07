using System.Text.Json;
using System.Text.Json.Nodes;

namespace AvroSourceGenerator.Tests.Helpers;

public static class TestSchemas
{
    private static readonly Dictionary<string, string> s_schemas = new()
    {
        ["enum"] = """
        {
            "type": "enum",
            "name": "Enum",
            "namespace": "SchemaNamespace",
            "symbols": []
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

    public static JsonNode Enum => Get("enum");
    public static JsonNode Error => Get("error");
    public static JsonNode Fixed => Get("fixed");
    public static JsonNode Record => Get("record");
    public static JsonNode ArrayOfString => Get("array<string>");
    public static JsonNode ArrayOfRecord => Get("array<record>");
    public static JsonNode MapOfString => Get("map<string>");
    public static JsonNode MapOfRecord => Get("map<record>");
    public static JsonNode UnionOfNullAndString => Get("[null, string]");
    public static JsonNode UnionOfNullAndRecord => Get("[null, record]");
    public static JsonNode Protocol => Get("protocol");

    public static JsonNode With(this JsonNode @this, string propertyName, JsonNode propertyValue)
    {
        var clone = @this.DeepClone();
        clone[propertyName] = propertyValue;
        return clone;
    }

    public static JsonNode With(this JsonNode @this, string propertyName, IEnumerable<object> propertyValue)
    {
        var clone = @this.DeepClone();
        clone[propertyName] = propertyValue is null ? null : new JsonArray([.. propertyValue.Select(Parse)]);
        return clone;

        static JsonNode Parse(object x) => x is null ? JsonNode.Parse("null")! : JsonNode.Parse(JsonSerializer.Serialize(x, x.GetType()))!;
    }
}
