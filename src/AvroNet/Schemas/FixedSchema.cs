using System.Text.Json;

namespace AvroNet.Schemas;

internal readonly record struct FixedSchema(JsonElement Json) : IAvroSchema
{
    public JsonElement Name { get => Json.GetProperty("name"); }
    public JsonElement Type { get => Json.GetProperty("type"); }
    public JsonElement Size { get => Json.GetProperty("size"); }
    public JsonElement? Namespace { get => Json.TryGetProperty("namespace", out var v) ? v : null; }
    public JsonElement? Documentation { get => Json.TryGetProperty("doc", out var v) ? v : null; }
    public JsonElement? Aliases { get => Json.TryGetProperty("aliases", out var v) ? v : null; }
}
