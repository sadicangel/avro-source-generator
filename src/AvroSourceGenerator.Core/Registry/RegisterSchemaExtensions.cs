using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Exceptions;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

internal static class RegisterSchemaExtensions
{
    extension(in SchemaRegistry schemaRegistry)
    {
        public AvroSchema Schema(JsonElement schema, string? containingNamespace)
        {
            return schema.ValueKind switch
            {
                JsonValueKind.String => schemaRegistry.Find(schema.ToRequiredString().ToSchemaName(containingNamespace)) ?? throw new InvalidSchemaException($"Unknown schema '{schema.ToRequiredString()}'"),
                JsonValueKind.Object => schemaRegistry.Complex(schema, containingNamespace),
                JsonValueKind.Array => schemaRegistry.Union(schema, containingNamespace),
                _ => throw new InvalidSchemaException($"Invalid schema: {schema.GetRawText()}")
            };
        }

        public AvroSchema Complex(JsonElement schema, string? containingNamespace)
        {
            if (schema.TryGetProperty(AvroJsonKeys.Protocol, out _))
            {
                return schemaRegistry.Protocol(schema, containingNamespace);
            }

            var type = schema.GetSchemaType();

            var underlyingSchema = type switch
            {
                AvroTypeNames.Array => schemaRegistry.Array(schema, containingNamespace),
                AvroTypeNames.Map => schemaRegistry.Map(schema, containingNamespace),
                AvroTypeNames.Enum => schemaRegistry.Enum(schema, containingNamespace),
                AvroTypeNames.Record => schemaRegistry.Record(schema, containingNamespace),
                AvroTypeNames.Error => schemaRegistry.Error(schema, containingNamespace),
                AvroTypeNames.Fixed => schemaRegistry.Fixed(schema, containingNamespace),
                _ => schemaRegistry.Find(type.ToSchemaName(containingNamespace)) ?? throw new InvalidSchemaException($"Unknown schema type '{type}' in {schema.GetRawText()}")
            };

            if (underlyingSchema is PrimitiveSchema primitive)
            {
                underlyingSchema = primitive with
                {
                    Documentation = schema.GetDocumentation(),
                    Properties = schema.GetSchemaProperties(),
                };
            }

            // TODO: Should we add/merge properties for other schema types?

            if (schema.GetLogicalType() is { } logicalType)
            {
                return LogicalSchema.Create(logicalType, underlyingSchema, schemaRegistry.Options.TargetProfile);
            }

            return underlyingSchema;
        }

        private ArraySchema Array(JsonElement schema, string? containingNamespace)
        {
            var itemsSchema = schema.GetRequiredProperty(AvroJsonKeys.Items);
            var items = schemaRegistry.Schema(itemsSchema, containingNamespace);
            var documentation = schema.GetDocumentation();
            var properties = schema.GetSchemaProperties();
            return new ArraySchema(items, documentation, properties);
        }

        private MapSchema Map(JsonElement schema, string? containingNamespace)
        {
            var valuesSchema = schema.GetRequiredProperty(AvroJsonKeys.Values);
            var values = schemaRegistry.Schema(valuesSchema, containingNamespace);
            var documentation = schema.GetDocumentation();
            var properties = schema.GetSchemaProperties();
            return new MapSchema(values, documentation, properties);
        }

        public EnumSchema Enum(JsonElement schema, string? containingNamespace)
        {
            var schemaName = schema.GetRequiredSchemaName(containingNamespace);
            using (schemaRegistry.EnterRecursionScope(schemaName))
            {
                var documentation = schema.GetDocumentation();
                var aliases = schema.GetAliases();
                var symbols = schema.GetSymbols();
                var @default = schema.GetNullableString(AvroJsonKeys.Default);
                var properties = schema.GetSchemaProperties();

                var enumSchema = new EnumSchema(schema, schemaName, documentation, aliases, symbols, @default, properties);
                schemaRegistry.Register(enumSchema);
                return enumSchema;
            }
        }

        public FixedSchema Fixed(JsonElement schema, string? containingNamespace)
        {
            var schemaName = schema.GetRequiredSchemaName(containingNamespace);
            using (schemaRegistry.EnterRecursionScope(schemaName))
            {
                var documentation = schema.GetDocumentation();
                var aliases = schema.GetAliases();
                var size = schema.GetFixedSize();
                var properties = schema.GetSchemaProperties();

                var fixedSchema = schemaRegistry.Options.TargetProfile switch
                {
                    // Only Apache.Avro needs a custom type for fixed, others use byte[].
                    TargetProfile.Apache => new FixedSchema(schema, schemaName, documentation, aliases, size, properties),
                    _ => FixedSchema.CreateAsByteArray(schema, schemaName, documentation, aliases, size, properties),
                };
                schemaRegistry.Register(fixedSchema);
                return fixedSchema;
            }
        }

        public ErrorSchema Error(JsonElement schema, string? containingNamespace)
        {
            var schemaName = schema.GetRequiredSchemaName(containingNamespace);
            using (schemaRegistry.EnterRecursionScope(schemaName))
            {
                var documentation = schema.GetDocumentation();
                var aliases = schema.GetAliases();
                var fields = schemaRegistry.Fields(schema, schemaName);
                var properties = schema.GetSchemaProperties();

                var errorSchema = new ErrorSchema(schema, schemaName, documentation, aliases, fields, properties);
                schemaRegistry.Register(errorSchema);
                return errorSchema;
            }
        }

        public RecordSchema Record(JsonElement schema, string? containingNamespace)
        {
            var schemaName = schema.GetRequiredSchemaName(containingNamespace);
            using (schemaRegistry.EnterRecursionScope(schemaName))
            {
                var documentation = schema.GetDocumentation();
                var aliases = schema.GetAliases();
                var fields = schemaRegistry.Fields(schema, schemaName);
                var properties = schema.GetSchemaProperties();

                var recordSchema = new RecordSchema(schema, schemaName, documentation, aliases, fields, properties);
                schemaRegistry.Register(recordSchema);
                return recordSchema;
            }
        }

        private ImmutableArray<Field> Fields(JsonElement schema, SchemaName containingSchemaName)
        {
            var fields = ImmutableArray.CreateBuilder<Field>();
            foreach (var field in schema.GetRequiredArray(AvroJsonKeys.Fields))
                fields.Add(schemaRegistry.Field(field, containingSchemaName));

            return fields.ToImmutable();
        }

        private Field Field(JsonElement field, SchemaName containingSchemaName)
        {
            var name = field.GetRequiredString(AvroJsonKeys.Name).ToValidName();
            var type = schemaRegistry.Schema(field.GetRequiredProperty(AvroJsonKeys.Type), containingSchemaName.Namespace);
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
                            // If a variant with the same name already exists, it has the same set of types, so we can just reuse it.
                            if (!schemaRegistry.Schemas.ContainsKey(variant.SchemaName))
                                schemaRegistry.Register(variant);

                            remarks = variant.Documentation;
                            union = union.WithVariant(variant);
                        }

                        type = union;
                        isNullable = union.IsNullable;
                        underlyingType = union.UnderlyingSchema;
                    }
                    break;

                case FixedSchema @fixed when schemaRegistry.Options.TargetProfile is not TargetProfile.Apache:
                    {
                        remarks = @fixed.Documentation;
                    }
                    break;
            }

            var documentation = field.GetDocumentation();
            var aliases = field.GetAliases();
            var defaultJson = field.GetNullableProperty(AvroJsonKeys.Default);
            var @default = type.GetValue(defaultJson);
            var order = field.GetNullableInt32(AvroJsonKeys.Order);
            var properties = field.GetSchemaProperties();

            return new Field(name, type, underlyingType, isNullable, documentation, aliases, defaultJson, @default, order, properties, remarks);
        }

        public UnionSchema Union(JsonElement schema, string? containingNamespace)
        {
            var builder = ImmutableArray.CreateBuilder<AvroSchema>();
            foreach (var innerSchema in schema.EnumerateArray())
                builder.Add(schemaRegistry.Schema(innerSchema, containingNamespace));
            var schemas = builder.ToImmutable();

            var underlyingSchema = GetUnderlyingSchema(schemas);
            var isNullable = schemas.Any(static schema => schema.Type == SchemaType.Null)
                && (schemaRegistry.Options.UseNullableReferenceTypes || MapsToValueType(underlyingSchema.Type));
            var csharpName = new CSharpName(
                isNullable ? $"{underlyingSchema.CSharpName.Name}?" : underlyingSchema.CSharpName.Name,
                underlyingSchema.CSharpName.Namespace);

            return new UnionSchema(csharpName, schemas, underlyingSchema, isNullable);

            static bool MapsToValueType(SchemaType type) =>
                type is SchemaType.Boolean or SchemaType.Int or SchemaType.Long or SchemaType.Float or SchemaType.Double or SchemaType.Enum;

            static AvroSchema GetUnderlyingSchema(ImmutableArray<AvroSchema> schemas)
            {
                var underlyingSchema = schemas switch
                {
                    // T1
                    [var t1] => t1,
                    // T1 | "null"
                    [{ Type: not SchemaType.Null } t1, { Type: SchemaType.Null }] => t1,
                    // "null" | T2
                    [{ Type: SchemaType.Null }, { Type: not SchemaType.Null } t2] => t2,
                    // T1 | T2 | ... | Tn
                    _ => AvroSchema.Object,
                };

                while (underlyingSchema is UnionSchema { Schemas: var unionSchemas })
                    underlyingSchema = GetUnderlyingSchema(unionSchemas);

                return underlyingSchema;
            }
        }
    }
}
