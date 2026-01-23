namespace AvroSourceGenerator.IntegrationTests.Chr;

public static class ConfluentSchemaExtensions
{
    extension<T>(T)
    {
        public static Confluent.SchemaRegistry.Schema GetSchema()
        {
            var schema = File.ReadAllText($"Schemas/{typeof(T).Name}.avsc");
            return new Confluent.SchemaRegistry.Schema(schema, Confluent.SchemaRegistry.SchemaType.Avro);
        }
    }
}
