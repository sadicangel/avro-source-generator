using System.Text.Json;

namespace AvroNet.Schemas;

internal readonly record struct LogicalSchema(JsonElement Json) : IAvroSchema
{
    public JsonElement Name { get => Json.GetProperty("name"); }
    public JsonElement Type { get => Json.GetProperty("type"); }
    public JsonElement LogicalType { get => Json.GetProperty("logicalType"); }
    public JsonElement? Documentation { get => Json.TryGetProperty("doc", out var v) ? v : null; }
}
