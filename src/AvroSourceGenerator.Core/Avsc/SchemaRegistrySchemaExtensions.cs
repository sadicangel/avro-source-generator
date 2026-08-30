using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Exceptions;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Registry;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Avsc;

public static class SchemaRegistrySchemaExtensions
{
    extension(in SchemaRegistry schemaRegistry)
    {
        public void RegisterSchema(JsonElement schema)
        {
            using (schemaRegistry.EnterRegisterScope())
            {
                var registeredSchema = schemaRegistry.Schema(schema, containingNamespace: null);
                if (!registeredSchema.ContainsTopLevelSchema())
                {
                    throw new InvalidSchemaException($"At least a named schema must be present in schema: {schema.GetRawText()}");
                }
            }
        }

        private AvroSchema Schema(JsonElement schema, string? containingNamespace)
        {
            return schema.ValueKind switch
            {
                JsonValueKind.String => schemaRegistry.Named(schema.ToRequiredString().ToSchemaName(), containingNamespace),
                JsonValueKind.Object => schemaRegistry.Complex(schema, containingNamespace),
                JsonValueKind.Array => schemaRegistry.Union(schema, containingNamespace),
                _ => throw new InvalidSchemaException($"Invalid schema: {schema.GetRawText()}")
            };
        }

        private AvroSchema Named(SchemaName schemaName, string? containingNamespace)
        {
            if (schemaRegistry.Find(schemaName, containingNamespace) is { } named)
            {
                return named;
            }

            schemaName = schemaName.ResolveIn(containingNamespace);
            if (schemaRegistry.Options.ReferenceResolution is ReferenceResolution.Strict)
            {
                throw new MissingReferenceException(schemaName);
            }

            schemaRegistry.AddReference(schemaName);

            return schemaRegistry.Find(schemaName, containingNamespace: null)
                ?? throw new InvalidOperationException("Unreachable code: reference should have been added in the registry.");
        }

        private AvroSchema Complex(JsonElement schema, string? containingNamespace)
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
                _ => schemaRegistry.Named(type.ToSchemaName(), containingNamespace)
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

        private EnumSchema Enum(JsonElement schema, string? containingNamespace)
        {
            var schemaName = schema.GetRequiredSchemaName(containingNamespace);
            using (schemaRegistry.EnterRecursionScope(schemaName))
            {
                var documentation = schema.GetDocumentation();
                var aliases = schema.GetAliases();
                var symbols = schema.GetSymbols();
                var @default = schema.GetNullableString(AvroJsonKeys.Default);
                var properties = schema.GetSchemaProperties();

                var enumSchema = new EnumSchema(schemaName, documentation, aliases, symbols, @default, properties);
                schemaRegistry.Register(enumSchema);
                return enumSchema;
            }
        }

        private FixedSchema Fixed(JsonElement schema, string? containingNamespace)
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
                    TargetProfile.Apache => new FixedSchema(schemaName, documentation, aliases, size, properties),
                    _ => FixedSchema.CreateAsByteArray(schemaName, documentation, aliases, size, properties),
                };
                schemaRegistry.Register(fixedSchema);
                return fixedSchema;
            }
        }

        private ErrorSchema Error(JsonElement schema, string? containingNamespace)
        {
            var schemaName = schema.GetRequiredSchemaName(containingNamespace);
            using (schemaRegistry.EnterRecursionScope(schemaName))
            {
                var documentation = schema.GetDocumentation();
                var aliases = schema.GetAliases();
                var fields = schemaRegistry.Fields(schema, schemaName);
                var properties = schema.GetSchemaProperties();

                var errorSchema = new ErrorSchema(schemaName, documentation, aliases, fields, properties);
                schemaRegistry.Register(errorSchema);
                return errorSchema;
            }
        }

        private RecordSchema Record(JsonElement schema, string? containingNamespace)
        {
            var schemaName = schema.GetRequiredSchemaName(containingNamespace);
            using (schemaRegistry.EnterRecursionScope(schemaName))
            {
                var documentation = schema.GetDocumentation();
                var aliases = schema.GetAliases();
                var fields = schemaRegistry.Fields(schema, schemaName);
                var properties = schema.GetSchemaProperties();

                var recordSchema = new RecordSchema(schemaName, documentation, aliases, fields, properties);
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
            type = schemaRegistry.ResolveFieldType(type, name, containingSchemaName, out var underlyingType, out var remarks);

            var documentation = field.GetDocumentation();
            var aliases = field.GetAliases();
            var defaultJson = field.GetNullableProperty(AvroJsonKeys.Default);
            var @default = type.GetValue(defaultJson);
            var order = field.GetOptionalString(AvroJsonKeys.Order);
            var properties = field.GetSchemaProperties();

            return new Field(name, type, underlyingType, documentation, aliases, defaultJson, @default, order, properties, remarks);
        }

        private UnionSchema Union(JsonElement schema, string? containingNamespace)
        {
            var builder = ImmutableArray.CreateBuilder<AvroSchema>();
            foreach (var innerSchema in schema.EnumerateArray())
                builder.Add(schemaRegistry.Schema(innerSchema, containingNamespace));
            var schemas = builder.ToImmutable();

            return UnionSchema.Create(schemas, schemaRegistry.Options.UseNullableReferenceTypes);
        }

        private ProtocolSchema Protocol(JsonElement schema, string? containingNamespace)
        {
            var schemaName = schema.GetRequiredProtocolName(containingNamespace);
            using (schemaRegistry.EnterRecursionScope(schemaName))
            {
                var documentation = schema.GetDocumentation();
                var types = schemaRegistry.ProtocolTypes(schema.GetRequiredArray(AvroJsonKeys.Types), schemaName.Namespace);
                var messages = schemaRegistry.ProtocolMessages(schema.GetRequiredObject(AvroJsonKeys.Messages), schemaName.Namespace);
                var properties = schema.GetProtocolProperties();

                var protocolSchema = new ProtocolSchema(schemaName, documentation, types, messages, properties);

                schemaRegistry.Register(protocolSchema);

                return protocolSchema;
            }
        }

        private ImmutableArray<NamedSchema> ProtocolTypes(JsonElement.ArrayEnumerator schemas, string? containingNamespace)
        {
            var types = ImmutableArray.CreateBuilder<NamedSchema>();
            foreach (var type in schemas)
                types.Add(schemaRegistry.NamedSchema(type, containingNamespace));

            return types.ToImmutable();
        }

        private NamedSchema NamedSchema(JsonElement schema, string? containingNamespace)
        {
            var type = schema.GetSchemaType();

            return type switch
            {
                AvroTypeNames.Enum => schemaRegistry.Enum(schema, containingNamespace),
                AvroTypeNames.Record => schemaRegistry.Record(schema, containingNamespace),
                AvroTypeNames.Error => schemaRegistry.Error(schema, containingNamespace),
                AvroTypeNames.Fixed => schemaRegistry.Fixed(schema, containingNamespace),
                _ => throw new InvalidSchemaException($"Unknown schema type '{type}' in {schema.GetRawText()}")
            };
        }

        private ImmutableArray<ProtocolMessage> ProtocolMessages(JsonElement.ObjectEnumerator messages, string? containingNamespace)
        {
            var protocolMessages = ImmutableArray.CreateBuilder<ProtocolMessage>();
            foreach (var message in messages)
                protocolMessages.Add(schemaRegistry.Message(message, containingNamespace));
            return protocolMessages.ToImmutable();
        }

        private ProtocolMessage Message(JsonProperty message, string? containingNamespace)
        {
            var methodName = message.Name.ToValidName();
            var documentation = message.Value.GetDocumentation();
            var requestParameters = schemaRegistry.ProtocolRequestParameters(message.Value, containingNamespace);
            var response = schemaRegistry.ProtocolResponse(message.Value.GetRequiredProperty(AvroJsonKeys.Response), containingNamespace);
            var errors = schemaRegistry.ProtocolErrors(message.Value.GetNullableArray(AvroJsonKeys.Errors), containingNamespace);
            var oneWay = message.Value.GetNullableBoolean(AvroJsonKeys.OneWay);
            if (oneWay is true && (response.Type.Type is not SchemaType.Null || errors.Length > 0))
            {
                throw new InvalidSchemaException($"One-way protocol message '{message.Name}' must have a null response and no errors in schema: {message.Value.GetRawText()}");
            }

            return new ProtocolMessage(methodName, documentation, requestParameters, response, errors, oneWay);
        }

        private ImmutableArray<ProtocolRequestParameter> ProtocolRequestParameters(JsonElement schema, string? containingNamespace)
        {
            var fields = ImmutableArray.CreateBuilder<ProtocolRequestParameter>();
            foreach (var parameter in schema.GetRequiredArray(AvroJsonKeys.Request))
                fields.Add(schemaRegistry.ProtocolRequestParameter(parameter, containingNamespace));

            return fields.ToImmutable();
        }

        private ProtocolRequestParameter ProtocolRequestParameter(JsonElement parameter, string? containingNamespace)
        {
            var name = parameter.GetRequiredString(AvroJsonKeys.Name).ToValidName();
            var type = schemaRegistry.Schema(parameter.GetRequiredProperty(AvroJsonKeys.Type), containingNamespace);
            var underlyingType = type is UnionSchema union ? union.UnderlyingSchema : type;

            var documentation = parameter.GetDocumentation();
            var defaultJson = parameter.GetNullableProperty(AvroJsonKeys.Default);
            var @default = type.GetValue(defaultJson);
            return new ProtocolRequestParameter(name, type, underlyingType, documentation, defaultJson, @default);
        }

        private ProtocolResponse ProtocolResponse(JsonElement schema, string? containingNamespace)
        {
            var type = schemaRegistry.Schema(schema, containingNamespace);
            var underlyingType = type is UnionSchema union ? union.UnderlyingSchema : type;

            return new ProtocolResponse(type, underlyingType);
        }

        private ImmutableArray<AvroSchema> ProtocolErrors(JsonElement.ArrayEnumerator? errors, string? containingNamespace)
        {
            if (errors is null)
            {
                return [];
            }

            var builder = ImmutableArray.CreateBuilder<AvroSchema>();
            foreach (var error in errors.Value)
            {
                builder.Add(schemaRegistry.Schema(error, containingNamespace));
            }

            return builder.ToImmutable();
        }
    }
}
