using System.Text.Json;

namespace AvroSourceGenerator.Schemas.Defaults;

public closed record class DefaultValue
{
    public abstract void WriteTo(Utf8JsonWriter writer);
}
