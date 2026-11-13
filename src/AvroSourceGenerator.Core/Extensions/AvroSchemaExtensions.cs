using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Extensions;

internal static class AvroSchemaExtensions
{
    extension(AvroSchema schema)
    {
        public bool ContainsTopLevelSchema()
        {
            return schema switch
            {
                TopLevelSchema or AvroSchemaReference => true,
                ArraySchema array => array.ItemSchema.ContainsTopLevelSchema(),
                MapSchema map => map.ValueSchema.ContainsTopLevelSchema(),
                UnionSchema union => union.Schemas.Any(ContainsTopLevelSchema),
                LogicalSchema logical => logical.UnderlyingSchema.ContainsTopLevelSchema(),
                _ => false
            };
        }

        public AvroSchema GetUnderlyingOrSelf() => schema switch
        {
            UnionSchema union => union.UnderlyingSchema,
            LogicalSchema logical => logical.UnderlyingSchema,
            _ => schema
        };
    }
}
