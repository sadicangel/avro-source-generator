using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
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
        var name = field.GetRequiredString("name").GetValidName();
        var type = Schema(field.GetRequiredProperty("type"), containingSchemaName.Namespace);
        var underlyingType = type;
        var isNullable = false;
        string? remarks = null;

        if (type is UnionSchema union)
        {
            // TODO: Can we extend this to Fixed and Error types in the future?
            if (IsEligibleForAbstractRecord(union))
            {
                var underlyingSchema = new AbstractRecordSchema(
                    SchemaName: new SchemaName(
                        Name: MakeVariantName(containingSchemaName.Name, name),
                        Namespace: containingSchemaName.Namespace),
                    DerivedSchemas: union.Schemas);

                union = union with
                {
                    CSharpName = new CSharpName(
                        underlyingSchema.CSharpName.Name + (union.IsNullable ? "?" : ""),
                        underlyingSchema.CSharpName.Namespace),
                    UnderlyingSchema = underlyingSchema
                };

                _schemas.Add(underlyingSchema.SchemaName, underlyingSchema);

                remarks = underlyingSchema.Documentation;

                foreach (var record in union.Schemas.OfType<RecordSchema>())
                {
                    record.InheritsFrom = underlyingSchema;
                }
            }

            type = union;
            isNullable = union.IsNullable;
            underlyingType = union.UnderlyingSchema;
        }

        var documentation = field.GetDocumentation();
        var aliases = field.GetAliases();
        var defaultJson = field.GetNullableProperty("default");
        var @default = GetValue(type, defaultJson);
        var order = field.GetNullableInt32("order");
        var properties = GetProperties(field);

        return new Field(name, type, underlyingType, isNullable, documentation, aliases, defaultJson, @default, order, properties, remarks);

        static string MakeVariantName(string schemaName, string fieldName)
        {
            var builder = new StringBuilder(schemaName.Length + fieldName.Length + 7)
                .Append(schemaName)
                .Append(fieldName)
                .Append("Variant");

            builder[schemaName.Length] = char.ToUpperInvariant(builder[schemaName.Length]);

            return builder.ToString();
        }

        static bool IsEligibleForAbstractRecord(UnionSchema union)
        {
            if (union.Schemas is [] or [_] or [{ Type: SchemaType.Null }, _] or [_, { Type: SchemaType.Null }])
            {
                // Empty union, single type union, or union with nulls only are not eligible for generating abstract base records.
                return false;
            }

            // Check if all schemas are either Record or Null, and at least one is not Null.
            return union.Schemas.All(x => x.Type is SchemaType.Record or SchemaType.Null)
                && union.Schemas.Any(x => x.Type is not SchemaType.Null);
        }
    }
}
