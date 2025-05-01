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
    };

    public static JsonNode Get(string schemaType) => JsonNode.Parse(s_schemas[schemaType])!;

    public static JsonNode Enum => Get("enum");
    public static JsonNode Error => Get("error");
    public static JsonNode Fixed => Get("fixed");
    public static JsonNode Record => Get("record");

    public static JsonNode With(this JsonNode @this, string propertyName, JsonNode value)
    {
        var clone = @this.DeepClone();
        clone[propertyName] = value;
        return clone;
    }
}
