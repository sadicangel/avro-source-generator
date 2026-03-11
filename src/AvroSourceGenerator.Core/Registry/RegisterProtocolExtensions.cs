using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Exceptions;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

internal static class RegisterProtocolExtensions
{
    extension(ref SchemaRegistry schemaRegistry)
    {
        public ProtocolSchema Protocol(JsonElement schema, string? containingNamespace)
        {
            var schemaName = schema.GetRequiredProtocolName(containingNamespace);
            using (schemaRegistry.EnterRecursionScope(schemaName))
            {
                var documentation = schema.GetDocumentation();
                var types = schemaRegistry.ProtocolTypes(schema.GetRequiredArray(AvroJsonKeys.Types), schemaName.Namespace);
                var messages = schemaRegistry.ProtocolMessages(schema.GetRequiredObject(AvroJsonKeys.Messages), schemaName.Namespace);
                var properties = schema.GetProtocolProperties();

                var protocolSchema = new ProtocolSchema(schema, schemaName, documentation, types, messages, properties);

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

        private ProtocolMessage Message(JsonProperty property, string? containingNamespace)
        {
            var methodName = property.Name.ToValidName();
            var documentation = property.Value.GetDocumentation();
            var requestParameters = schemaRegistry.ProtocolRequestParameters(property.Value, containingNamespace);
            var response = schemaRegistry.ProtocolResponse(property.Value.GetRequiredProperty(AvroJsonKeys.Response), containingNamespace);
            var errors = schemaRegistry.ProtocolErrors(property.Value.GetNullableArray(AvroJsonKeys.Errors), containingNamespace);
            return new ProtocolMessage(methodName, documentation, requestParameters, response, errors);
        }

        private ImmutableArray<ProtocolRequestParameter> ProtocolRequestParameters(JsonElement schema, string? containingNamespace)
        {
            var fields = ImmutableArray.CreateBuilder<ProtocolRequestParameter>();
            foreach (var parameter in schema.GetRequiredArray(AvroJsonKeys.Request))
                fields.Add(schemaRegistry.ProtocolRequestParameter(parameter, containingNamespace));

            return fields.ToImmutable();
        }

        private ProtocolRequestParameter ProtocolRequestParameter(JsonElement field, string? containingNamespace)
        {
            var name = field.GetRequiredString(AvroJsonKeys.Name).ToValidName();
            var type = schemaRegistry.Schema(field.GetRequiredProperty(AvroJsonKeys.Type), containingNamespace);
            var underlyingType = type;
            var isNullable = false;
            if (type is UnionSchema union)
            {
                isNullable = union.IsNullable;
                underlyingType = union.UnderlyingSchema;
            }

            var documentation = field.GetDocumentation();
            var defaultJson = field.GetNullableProperty(AvroJsonKeys.Default);
            var @default = type.GetValue(defaultJson);
            return new ProtocolRequestParameter(name, type, underlyingType, isNullable, documentation, defaultJson, @default);
        }

        private ProtocolResponse ProtocolResponse(JsonElement schema, string? containingNamespace)
        {
            var type = schemaRegistry.Schema(schema, containingNamespace);
            var underlyingType = type;
            var isNullable = false;
            if (type is UnionSchema union)
            {
                isNullable = union.IsNullable;
                underlyingType = union.UnderlyingSchema;
            }

            return new ProtocolResponse(type, underlyingType, isNullable);
        }

        private ImmutableArray<AvroSchema> ProtocolErrors(JsonElement.ArrayEnumerator? errors, string? containingNamespace)
        {
            var builder = ImmutableArray.CreateBuilder<AvroSchema>();
            if (errors is null)
            {
                return builder.ToImmutable();
            }

            foreach (var error in errors.Value)
            {
                builder.Add(schemaRegistry.Schema(error, containingNamespace));
            }

            return builder.ToImmutable();
        }
    }
}
