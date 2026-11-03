using System.Text.Json;

namespace AvroSourceGenerator.Schemas.Defaults;

public sealed record class UnboundDefaultValue(JsonElement Json) : DefaultValue
{
    public JsonElement Json { get; init; } = Json.Clone();

    public bool Equals(UnboundDefaultValue? other) => other is not null
        && string.Equals(Json.GetRawText(), other.Json.GetRawText(), StringComparison.Ordinal);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Json.GetRawText());

    public override void WriteTo(Utf8JsonWriter writer) => writer.WriteRawValue(Json.GetRawText());
}
