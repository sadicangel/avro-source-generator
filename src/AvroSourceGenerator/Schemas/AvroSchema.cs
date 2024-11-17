using System.Text.Json;

namespace AvroSourceGenerator.Schemas;
internal readonly record struct AvroSchema(JsonElement Json) : IAvroSchema
{
    public JsonElement Name
    {
        get => Json.ValueKind switch
        {
            JsonValueKind.String => Json,
            JsonValueKind.Object => Json.TryGetProperty("name", out var name) ? name : Json.GetProperty("type"),
            JsonValueKind.Array => UnionSchema.SyntheticDocument.RootElement,
            _ => throw new InvalidOperationException($"Invalid schema {Json.GetRawText()}")
        };
    }
    public JsonElement Type
    {
        get => Json.ValueKind switch
        {
            JsonValueKind.String => Json,
            JsonValueKind.Object => Json.GetProperty("type"),
            JsonValueKind.Array => UnionSchema.SyntheticDocument.RootElement,
            _ => throw new InvalidOperationException($"Invalid schema {Json.GetRawText()}")
        };
    }

    public PrimitiveSchema AsPrimitiveSchema() => new(Json);
    public ArraySchema AsArraySchema() => new(Json);
    public MapSchema AsMapSchema() => new(Json);
    public UnionSchema AsUnionSchema() => new(Json);
    public RecordSchema AsRecordSchema() => new(Json);
    public ErrorSchema AsErrorSchema() => new(Json);
    public EnumSchema AsEnumSchema() => new(Json);
    public FixedSchema AsFixedSchema() => new(Json);
    public LogicalSchema AsLogicalSchema() => new(Json);
}
