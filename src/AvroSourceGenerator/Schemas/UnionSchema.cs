using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal readonly record struct UnionSchema(JsonElement Json) : IAvroSchema
{
    public static readonly JsonDocument SyntheticDocument = JsonDocument.Parse("\"union\"");
    public JsonElement Name { get => SyntheticDocument.RootElement; }
    public JsonElement Type { get => SyntheticDocument.RootElement; }
    public int Length { get => Json.GetArrayLength(); }
    public IEnumerable<AvroSchema> Schemas
    {
        get
        {
            var schemas = Json.EnumerateArray();
            while (schemas.MoveNext())
                yield return new(schemas.Current);
        }
    }
}
