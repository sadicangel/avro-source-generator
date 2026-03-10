using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

public readonly partial struct SchemaRegistry
{
    private ImmutableArray<Field> Fields(JsonElement schema, SchemaName containingSchemaName)
    {
        var fields = ImmutableArray.CreateBuilder<Field>();
        foreach (var field in schema.GetRequiredArray(AvroJsonKeys.Fields))
            fields.Add(Field(field, containingSchemaName));

        return fields.ToImmutable();
    }

    private Field Field(JsonElement field, SchemaName containingSchemaName)
    {
        var name = field.GetRequiredString(AvroJsonKeys.Name).ToValidName();
        var type = Schema(field.GetRequiredProperty(AvroJsonKeys.Type), containingSchemaName.Namespace);
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
                        // It is OK to ignore the result of TryRegister here. If a variant with the same name already exists
                        // it means that it has the same set of types in the union, so we can just reuse it.
                        _ = TryRegister(variant);

                        remarks = variant.Documentation;
                        union = union.WithVariant(variant);
                    }

                    type = union;
                    isNullable = union.IsNullable;
                    underlyingType = union.UnderlyingSchema;
                }
                break;

            case FixedSchema @fixed when options.TargetProfile is not TargetProfile.Apache:
                {
                    remarks = @fixed.Documentation;
                }
                break;
        }

        var documentation = field.GetDocumentation();
        var aliases = field.GetAliases();
        var defaultJson = field.GetNullableProperty(AvroJsonKeys.Default);
        var @default = GetValue(type, defaultJson);
        var order = field.GetNullableInt32(AvroJsonKeys.Order);
        var properties = field.GetSchemaProperties();

        return new Field(name, type, underlyingType, isNullable, documentation, aliases, defaultJson, @default, order, properties, remarks);
    }
}
