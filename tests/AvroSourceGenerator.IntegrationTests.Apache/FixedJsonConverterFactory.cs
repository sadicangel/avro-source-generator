using System.Text.Json;
using System.Text.Json.Serialization;
using Avro.Specific;

namespace AvroSourceGenerator.IntegrationTests.Apache;

internal sealed class FixedJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsAssignableTo(typeof(SpecificFixed));

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(FixedJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private sealed class FixedJsonConverter<T> : JsonConverter<T> where T : SpecificFixed
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException($"Expected string token, but got {reader.TokenType}.");
            var @fixed = Activator.CreateInstance<T>();
            @fixed.Value = Convert.FromBase64String(reader.GetString()!);
            return @fixed;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
            writer.WriteBase64StringValue(value.Value);
    }
}
