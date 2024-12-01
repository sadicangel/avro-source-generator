using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal readonly record struct ErrorSchema(JsonElement Json) : IAvroSchema
{
    public JsonElement Name { get => Json.GetProperty("name"); }
    public JsonElement Type { get => Json.GetProperty("type"); }
    public int FieldsLength { get => Json.GetProperty("fields").GetArrayLength(); }
    public IEnumerable<FieldSchema> Fields
    {
        get
        {
            var fields = Json.GetProperty("fields").EnumerateArray();
            while (fields.MoveNext())
                yield return new(fields.Current);
        }
    }
    public JsonElement? Namespace { get => Json.TryGetProperty("namespace", out var v) ? v : null; }
    public JsonElement? Documentation { get => Json.TryGetProperty("doc", out var v) ? v : null; }
    public int AliasesLength { get => Json.TryGetProperty("aliases", out var aliases) ? aliases.GetArrayLength() : 0; }
    public IEnumerable<JsonElement> Aliases
    {
        get
        {
            if (Json.TryGetProperty("aliases", out var aliases))
            {
                var array = aliases.EnumerateArray();
                while (array.MoveNext())
                    yield return array.Current;
            }
        }
    }
}
