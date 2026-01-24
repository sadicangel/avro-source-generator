using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Registry.Extensions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

internal readonly partial struct SchemaRegistry
{
    private ImmutableArray<Field> Fields(JsonElement schema, SchemaName containingSchemaName)
    {
        var fields = ImmutableArray.CreateBuilder<Field>();
        foreach (var field in schema.GetRequiredArray("fields"))
            fields.Add(Field(field, containingSchemaName));

        return fields.ToImmutable();
    }

    private Field Field(JsonElement field, SchemaName containingSchemaName)
    {
        var name = field.GetRequiredString("name").ToValidName();
        var type = Schema(field.GetRequiredProperty("type"), containingSchemaName.Namespace);
        var underlyingType = type;
        var isNullable = false;
        string? remarks = null;

        switch (type)
        {
            case UnionSchema union:
                {
                    if (union.SupportsVariant())
                    {
                        var variant = new VariantSchema(name, containingSchemaName, union.Schemas);
                        _schemas.Add(variant.SchemaName, variant);

                        remarks = variant.Documentation;
                        union = union.WithVariant(variant);
                    }

                    type = union;
                    isNullable = union.IsNullable;
                    underlyingType = union.UnderlyingSchema;
                }
                break;

            case FixedSchema @fixed when avroLibrary is not AvroLibrary.Apache:
                {
                    remarks = @fixed.Documentation;
                }
                break;
        }

        var documentation = field.GetDocumentation();
        var aliases = field.GetAliases();
        var defaultJson = field.GetNullableProperty("default");
        var @default = GetValue(type, defaultJson);
        var order = field.GetNullableInt32("order");
        var properties = GetProperties(field);

        return new Field(name, type, underlyingType, isNullable, documentation, aliases, defaultJson, @default, order, properties, remarks);
    }
}
