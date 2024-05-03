using System.Text.Json;

namespace AvroNet.Schemas;

internal readonly record struct ArraySchema(JsonElement Json) : IAvroSchema
{
    public JsonElement Name { get => Json.GetProperty("type"); }
    public JsonElement Type { get => Json.GetProperty("type"); }
    public JsonElement Items { get => Json.GetProperty("items"); }
    public AvroSchema ItemsSchema { get => new(Items); }
}
