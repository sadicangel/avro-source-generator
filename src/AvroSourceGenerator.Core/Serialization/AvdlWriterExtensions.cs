using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Serialization;

public static class AvdlWriterExtensions
{
    extension(AvroSchema schema)
    {
        public string ToAvdl(IReadOnlyDictionary<SchemaName, TopLevelSchema>? schemaIndex = null, CancellationToken cancellationToken = default) =>
            AvdlWriter.WriteToString(schema, schemaIndex, cancellationToken);
    }
}
