using System.Text.Json;
using AvroSourceGenerator.Exceptions;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

public static class SchemaRegistryExtensions
{
    extension(in SchemaRegistry schemaRegistry)
    {
        public void RegisterSchema(JsonElement schema)
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

        public void RegisterSubject(JsonElement subject)
        {
            var schemaJson = subject.GetRequiredString("schema");
            using (schemaRegistry.EnterRegisterScope())
            {
                foreach (var reference in subject.GetNullableArray("references") ?? [])
                {
                    schemaRegistry.AddReference(reference.GetRequiredString("name").ToSchemaName());
                }

                using var schemaDocument = JsonDocument.Parse(schemaJson);
                var registeredSchema = schemaRegistry.Schema(schemaDocument.RootElement, containingNamespace: null);
                if (!ContainsTopLevelSchema(registeredSchema))
                {
                    throw new InvalidSchemaException($"At least a named schema must be present in schema: {schemaDocument.RootElement.GetRawText()}");
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
