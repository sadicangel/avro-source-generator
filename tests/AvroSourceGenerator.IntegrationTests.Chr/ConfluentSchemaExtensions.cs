using Confluent.SchemaRegistry;

namespace AvroSourceGenerator.IntegrationTests.Chr;

public static class ConfluentSchemaExtensions
{
    extension<T>(T)
    {
        public static Schema GetSchema()
        {
            var schema = File.ReadAllText($"Schemas/{typeof(T).Name}.avsc");
            return new Schema(schema, SchemaType.Avro);
        }
    }
}
