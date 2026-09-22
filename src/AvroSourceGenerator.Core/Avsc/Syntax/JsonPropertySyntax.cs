namespace AvroSourceGenerator.Avsc.Syntax;

internal sealed record class JsonPropertySyntax(JsonPropertyNameSyntax Name, JsonSyntax Value)
{
    public JsonSyntax? Parent => Value.Parent;
}
