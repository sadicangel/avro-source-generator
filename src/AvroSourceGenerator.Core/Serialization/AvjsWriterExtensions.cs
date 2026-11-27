using System.Text.Json;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Serialization;

public static class AvjsWriterExtensions
{
    extension(AvroSchema schema)
    {
        public string ToAvjs(IReadOnlyDictionary<SchemaName, TopLevelSchema>? schemaIndex = null, JsonWriterOptions options = default) =>
            AvjsWriter.WriteToString(schema, schemaIndex, options);
    }
}
