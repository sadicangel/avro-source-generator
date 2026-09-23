namespace AvroSourceGenerator.Avjs;

public sealed record class JsonPropertySyntax(JsonPropertyNameSyntax Name, JsonSyntax Value)
{
    public JsonSyntax? Parent => Value.Parent;
}
