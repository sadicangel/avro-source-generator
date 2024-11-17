using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal readonly record struct PrimitiveSchema(JsonElement Json) : IAvroSchema
{
    public JsonElement Name { get => Json; }
    public JsonElement Type { get => Json; }
}
