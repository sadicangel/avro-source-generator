using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal readonly record struct MapSchema(JsonElement Json) : IAvroSchema
{
    public JsonElement Name { get => Json.GetProperty("type"); }
    public JsonElement Type { get => Json.GetProperty("type"); }
    public JsonElement Values { get => Json.GetProperty("values"); }
    public AvroSchema ValuesSchema { get => new(Values); }
}
