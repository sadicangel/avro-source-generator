using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Exceptions;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Avsc;

// TODO:
// We currently throw exceptions for invalid schemas. We should consider
// returning diagnostics instead, maybe sharing the same diagnostic model as Avdl.
public static class AvscSchemaParser
{
    public static ParseResult Parse(SourceText source, AvroParseOptions options)
    {
        using var schema = JsonDocument.Parse(source.Text);
        var context = new ParserContext(options);
        var root = context.Schema(schema.RootElement, containingNamespace: null);
        if (root is not AvroSchemaReference && !root.ContainsTopLevelSchema())
        {
            throw new InvalidSchemaException($"At least a named schema must be present in schema: {source.Text}");
        }

        return context.Complete(root);
    }

    extension(ParserContext context)
    {
        private AvroSchema Schema(JsonElement schema, string? containingNamespace)
        {
            return schema.ValueKind switch
            {
                JsonValueKind.String => context.Named(schema.ToRequiredString().ToSchemaName(), containingNamespace),
                JsonValueKind.Object => context.Complex(schema, containingNamespace),
                JsonValueKind.Array => context.Union(schema, containingNamespace),
                _ => throw new InvalidSchemaException($"Invalid schema: {schema.GetRawText()}")
            };
        }

        private AvroSchema Named(SchemaName schemaName, string? containingNamespace)
        {
            return context.Reference(schemaName, containingNamespace);
        }

        private AvroSchema Complex(JsonElement schema, string? containingNamespace)
        {
            if (schema.TryGetProperty(AvroJsonKeys.Protocol, out _))
            {
                return context.Protocol(schema, containingNamespace);
            }

            var type = schema.GetSchemaType();

            var underlyingSchema = type switch
            {
                AvroTypeNames.Array => context.Array(schema, containingNamespace),
                AvroTypeNames.Map => context.Map(schema, containingNamespace),
                AvroTypeNames.Enum => context.Enum(schema, containingNamespace),
                AvroTypeNames.Record => context.Record(schema, containingNamespace),
                AvroTypeNames.Error => context.Error(schema, containingNamespace),
                AvroTypeNames.Fixed => context.Fixed(schema, containingNamespace),
                _ => context.Named(type.ToSchemaName(), containingNamespace)
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
                return LogicalSchema.Create(logicalType, underlyingSchema, context.Options.TargetProfile);
            }

            return underlyingSchema;
        }

        private ArraySchema Array(JsonElement schema, string? containingNamespace)
        {
            var itemsSchema = schema.GetRequiredProperty(AvroJsonKeys.Items);
            var items = context.Schema(itemsSchema, containingNamespace);
            var documentation = schema.GetDocumentation();
            var properties = schema.GetSchemaProperties();
            return new ArraySchema(items, documentation, properties);
        }

        private MapSchema Map(JsonElement schema, string? containingNamespace)
        {
            var valuesSchema = schema.GetRequiredProperty(AvroJsonKeys.Values);
            var values = context.Schema(valuesSchema, containingNamespace);
            var documentation = schema.GetDocumentation();
            var properties = schema.GetSchemaProperties();
            return new MapSchema(values, documentation, properties);
        }

        private EnumSchema Enum(JsonElement schema, string? containingNamespace)
        {
            var schemaName = schema.GetRequiredSchemaName(containingNamespace);
            using (context.EnterRecursionScope(schemaName))
            {
                var documentation = schema.GetDocumentation();
                var aliases = schema.GetAliases();
                var symbols = schema.GetSymbols();
                var @default = schema.GetNullableString(AvroJsonKeys.Default);
                var properties = schema.GetSchemaProperties();

                var enumSchema = new EnumSchema(schemaName, documentation, aliases, symbols, @default, properties);
                context.Declare(enumSchema);
                return enumSchema;
            }
        }

        private FixedSchema Fixed(JsonElement schema, string? containingNamespace)
        {
            var schemaName = schema.GetRequiredSchemaName(containingNamespace);
            using (context.EnterRecursionScope(schemaName))
            {
                var documentation = schema.GetDocumentation();
                var aliases = schema.GetAliases();
                var size = schema.GetFixedSize();
                var properties = schema.GetSchemaProperties();

                var fixedSchema = context.Options.TargetProfile switch
                {
                    // Only Apache.Avro needs a custom type for fixed, others use byte[].
                    TargetProfile.Apache => new FixedSchema(schemaName, documentation, aliases, size, properties),
                    _ => FixedSchema.CreateAsByteArray(schemaName, documentation, aliases, size, properties),
                };
                context.Declare(fixedSchema);
                return fixedSchema;
            }
        }

        private ErrorSchema Error(JsonElement schema, string? containingNamespace)
        {
            var schemaName = schema.GetRequiredSchemaName(containingNamespace);
            using (context.EnterRecursionScope(schemaName))
            {
                var documentation = schema.GetDocumentation();
                var aliases = schema.GetAliases();
                var fields = context.Fields(schema, schemaName);
                var properties = schema.GetSchemaProperties();

                var errorSchema = new ErrorSchema(schemaName, documentation, aliases, fields, properties);
                context.Declare(errorSchema);
                return errorSchema;
            }
        }

        private RecordSchema Record(JsonElement schema, string? containingNamespace)
        {
            var schemaName = schema.GetRequiredSchemaName(containingNamespace);
            using (context.EnterRecursionScope(schemaName))
            {
                var documentation = schema.GetDocumentation();
                var aliases = schema.GetAliases();
                var fields = context.Fields(schema, schemaName);
                var properties = schema.GetSchemaProperties();

                var recordSchema = new RecordSchema(schemaName, documentation, aliases, fields, properties);
                context.Declare(recordSchema);
                return recordSchema;
            }
        }

        private ImmutableArray<Field> Fields(JsonElement schema, SchemaName containingSchemaName)
        {
            var items = schema.GetRequiredArray(AvroJsonKeys.Fields, out var count);
            var fields = ImmutableArray.CreateBuilder<Field>(count);
            foreach (var field in items)
                fields.Add(context.Field(field, containingSchemaName));

            return fields.MoveToImmutable();
        }

        private Field Field(JsonElement field, SchemaName containingSchemaName)
        {
            var name = field.GetRequiredString(AvroJsonKeys.Name).ToValidName();
            var type = context.Schema(field.GetRequiredProperty(AvroJsonKeys.Type), containingSchemaName.Namespace);
            type = context.ResolveFieldType(type, name, containingSchemaName, out var underlyingType, out var remarks);

            var documentation = field.GetDocumentation();
            var aliases = field.GetAliases();
            JsonElement? defaultJson = field.GetNullableProperty(AvroJsonKeys.Default) is { } fieldDefault
                ? fieldDefault.Clone()
                : null;
            var @default = type.GetValue(defaultJson);
            var order = field.GetOptionalString(AvroJsonKeys.Order);
            var properties = field.GetSchemaProperties();

            return new Field(name, type, underlyingType, documentation, aliases, defaultJson, @default, order, properties, remarks);
        }

        private UnionSchema Union(JsonElement schema, string? containingNamespace)
        {
            var builder = ImmutableArray.CreateBuilder<AvroSchema>(schema.GetArrayLength());
            foreach (var innerSchema in schema.EnumerateArray())
                builder.Add(context.Schema(innerSchema, containingNamespace));
            var schemas = builder.MoveToImmutable();

            return UnionSchema.Create(schemas, context.Options.UseNullableReferenceTypes);
        }

        private ProtocolSchema Protocol(JsonElement schema, string? containingNamespace)
        {
            var schemaName = schema.GetRequiredProtocolName(containingNamespace);
            using (context.EnterRecursionScope(schemaName))
            {
                var documentation = schema.GetDocumentation();
                var typeItems = schema.GetRequiredArray(AvroJsonKeys.Types, out var typeCount);
                var types = context.ProtocolTypes(typeItems, typeCount, schemaName.Namespace);
                var messages = context.ProtocolMessages(schema.GetRequiredObject(AvroJsonKeys.Messages), schemaName.Namespace);
                var properties = schema.GetProtocolProperties();

                var protocolSchema = new ProtocolSchema(schemaName, documentation, types, messages, properties);

                context.Declare(protocolSchema);

                return protocolSchema;
            }
        }

        private ImmutableArray<NamedSchema> ProtocolTypes(JsonElement.ArrayEnumerator schemas, int count, string? containingNamespace)
        {
            var types = ImmutableArray.CreateBuilder<NamedSchema>(count);
            foreach (var type in schemas)
                types.Add(context.NamedSchema(type, containingNamespace));

            return types.MoveToImmutable();
        }

        private NamedSchema NamedSchema(JsonElement schema, string? containingNamespace)
        {
            var type = schema.GetSchemaType();

            return type switch
            {
                AvroTypeNames.Enum => context.Enum(schema, containingNamespace),
                AvroTypeNames.Record => context.Record(schema, containingNamespace),
                AvroTypeNames.Error => context.Error(schema, containingNamespace),
                AvroTypeNames.Fixed => context.Fixed(schema, containingNamespace),
                _ => throw new InvalidSchemaException($"Unknown schema type '{type}' in {schema.GetRawText()}")
            };
        }

        private ImmutableArray<ProtocolMessage> ProtocolMessages(JsonElement.ObjectEnumerator messages, string? containingNamespace)
        {
            var protocolMessages = ImmutableArray.CreateBuilder<ProtocolMessage>();
            foreach (var message in messages)
                protocolMessages.Add(context.Message(message, containingNamespace));
            return protocolMessages.DrainToImmutable();
        }

        private ProtocolMessage Message(JsonProperty message, string? containingNamespace)
        {
            var methodName = message.Name.ToValidName();
            var documentation = message.Value.GetDocumentation();
            var requestParameters = context.ProtocolRequestParameters(message.Value, containingNamespace);
            var response = context.ProtocolResponse(message.Value.GetRequiredProperty(AvroJsonKeys.Response), containingNamespace);
            var errorItems = message.Value.GetNullableArray(AvroJsonKeys.Errors, out var errorCount);
            var errors = context.ProtocolErrors(errorItems, errorCount, containingNamespace);
            var oneWay = message.Value.GetNullableBoolean(AvroJsonKeys.OneWay);
            if (oneWay is true && (response.Type.Type is not SchemaType.Null || errors.Length > 0))
            {
                throw new InvalidSchemaException($"One-way protocol message '{message.Name}' must have a null response and no errors in schema: {message.Value.GetRawText()}");
            }

            return new ProtocolMessage(methodName, documentation, requestParameters, response, errors, oneWay);
        }

        private ImmutableArray<ProtocolRequestParameter> ProtocolRequestParameters(JsonElement schema, string? containingNamespace)
        {
            var parameters = schema.GetRequiredArray(AvroJsonKeys.Request, out var count);
            var fields = ImmutableArray.CreateBuilder<ProtocolRequestParameter>(count);
            foreach (var parameter in parameters)
                fields.Add(context.ProtocolRequestParameter(parameter, containingNamespace));

            return fields.MoveToImmutable();
        }

        private ProtocolRequestParameter ProtocolRequestParameter(JsonElement parameter, string? containingNamespace)
        {
            var name = parameter.GetRequiredString(AvroJsonKeys.Name).ToValidName();
            var type = context.Schema(parameter.GetRequiredProperty(AvroJsonKeys.Type), containingNamespace);
            var underlyingType = type is UnionSchema union ? union.UnderlyingSchema : type;

            var documentation = parameter.GetDocumentation();
            JsonElement? defaultJson = parameter.GetNullableProperty(AvroJsonKeys.Default) is { } parameterDefault
                ? parameterDefault.Clone()
                : null;
            var @default = type.GetValue(defaultJson);
            return new ProtocolRequestParameter(name, type, underlyingType, documentation, defaultJson, @default);
        }

        private ProtocolResponse ProtocolResponse(JsonElement schema, string? containingNamespace)
        {
            var type = context.Schema(schema, containingNamespace);
            var underlyingType = type is UnionSchema union ? union.UnderlyingSchema : type;

            return new ProtocolResponse(type, underlyingType);
        }

        private ImmutableArray<AvroSchema> ProtocolErrors(JsonElement.ArrayEnumerator? errors, int count, string? containingNamespace)
        {
            if (errors is null)
            {
                return [];
            }

            var builder = ImmutableArray.CreateBuilder<AvroSchema>(count);
            foreach (var error in errors.Value)
            {
                builder.Add(context.Schema(error, containingNamespace));
            }

            return builder.MoveToImmutable();
        }
    }
}
