using System.Text.Json;
using AvroSourceGenerator.Exceptions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

public static class SchemaRegistryExtensions
{
    extension(ref SchemaRegistry schemaRegistry)
    {
        public void Register(JsonElement schema)
        {
            using (schemaRegistry.EnterRegisterScope())
            {
                var registeredSchema = schemaRegistry.Schema(schema, containingNamespace: null);
                if (!ContainsTopLevelSchema(registeredSchema))
                {
                    throw new InvalidSchemaException($"At least a named schema must be present in schema: {schema.GetRawText()}");
                }
            }
        }
    }

    private static bool ContainsTopLevelSchema(AvroSchema schema)
    {
        return schema switch
        {
            TopLevelSchema => true,
            ArraySchema array => ContainsTopLevelSchema(array.ItemSchema),
            MapSchema map => ContainsTopLevelSchema(map.ValueSchema),
            UnionSchema union => union.Schemas.Any(ContainsTopLevelSchema),
            LogicalSchema logical => ContainsTopLevelSchema(logical.UnderlyingSchema),
            _ => false
        };
    }
}
