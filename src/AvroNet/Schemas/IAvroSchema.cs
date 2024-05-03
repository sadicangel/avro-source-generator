using System.Text.Json;

namespace AvroNet.Schemas;

internal interface IAvroSchema
{
    JsonElement Json { get; }
    JsonElement Name { get; }
    JsonElement Type { get; }
}
