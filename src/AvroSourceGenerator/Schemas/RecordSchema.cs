using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal readonly record struct RecordSchema(JsonElement Json) : IAvroSchema
{
    public JsonElement Name { get => Json.GetProperty("name"); }
    public JsonElement Type { get => Json.GetProperty("type"); }
    public IEnumerable<FieldSchema> Fields
    {
        get
        {
            var fields = Json.GetProperty("fields").EnumerateArray();
            while (fields.MoveNext())
                yield return new(fields.Current);
        }
    }
    public int FieldsLength { get => Json.GetProperty("fields").GetArrayLength(); }
    public JsonElement? Namespace { get => Json.TryGetProperty("namespace", out var v) ? v : null; }
    public JsonElement? Documentation { get => Json.TryGetProperty("doc", out var v) ? v : null; }
    public JsonElement? Aliases { get => Json.TryGetProperty("aliases", out var v) ? v : null; }
}
