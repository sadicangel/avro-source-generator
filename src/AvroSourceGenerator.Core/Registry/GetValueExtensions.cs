using System.Text.Json;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

internal static class GetValueExtensions
{
    extension(AvroSchema schema)
    {
        // TODO: This should probably be in AvroSchema hierarchy instead of being here.
        public string? GetValue(JsonElement? json)
        {
            if (json is null or { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined })
            {
                return null;
            }

            var value = json.Value;

            // TODO: Actually validate the value so that we don't generate invalid code.
            return schema.CSharpName.Name switch
            {
                "object" or "bool" or "int" or "long" => value.GetRawText(),
                "float" => $"{value.GetRawText()}f",
                "double" => value.GetRawText(),
                "byte[]" => $"[{string.Join(", ", value.GetBytesFromBase64().Select(bytes => $"0x{bytes:X2}"))}]",
                "string" => value.GetRawText(),
                _ when schema.Type is SchemaType.Enum => $"{schema}.{value.GetString()}",

                // TODO: Do we need to handle complex types? Should they be supported?
                _ => null,
            };
        }
    }
}
