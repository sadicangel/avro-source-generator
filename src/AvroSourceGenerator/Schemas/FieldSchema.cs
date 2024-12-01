using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal readonly record struct FieldSchema(JsonElement Json) : IAvroSchema
{
    public JsonElement Name { get => Json.GetProperty("name"); }
    public JsonElement Type { get => Json.GetProperty("type"); }
    public AvroSchema Schema { get => new(Type); }
    public JsonElement? Documentation { get => Json.TryGetProperty("doc", out var v) ? v : null; }
    public JsonElement? Default { get => Json.TryGetProperty("default", out var v) ? v : null; }
    public JsonElement? Order { get => Json.TryGetProperty("order", out var v) ? v : null; }
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
