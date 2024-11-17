using System.Text.Json;

namespace AvroSourceGenerator.Schemas;

internal interface IAvroSchema
{
    JsonElement Json { get; }
    JsonElement Name { get; }
    JsonElement Type { get; }
}
