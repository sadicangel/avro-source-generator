namespace AvroSourceGenerator.Schemas;

internal sealed record class ArraySchema(AvroSchema ItemSchema)
    : AvroSchema(SchemaType.Array, $"IList<{ItemSchema}>", "System.Collections.Generic");
