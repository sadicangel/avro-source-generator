using System.Collections.Frozen;
using System.Collections.Immutable;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Compiler;

internal sealed class SchemaBinder(LinkedAvroFile linkedFile, CancellationToken cancellationToken)
{
    private readonly AvroParseOptions _options = linkedFile.File.ParseOptions;
    private readonly FrozenDictionary<SchemaName, CSharpName?> _references = linkedFile.References;
    private readonly Dictionary<AvroSchema, AvroSchema> _boundSchemas = new(ReferenceEqualityComparer.Instance);

    public AvroSchema Bind(AvroSchema schema)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (schema is AvroSchemaReference reference)
            return BindReference(reference);

        if (_boundSchemas.TryGetValue(schema, out var existing))
            return existing;

        var bound = schema switch
        {
            ArraySchema array => BindArray(array),
            MapSchema map => BindMap(map),
            UnionSchema union => BindUnion(union),
            LogicalSchema logical => BindLogical(logical),
            RecordSchema record => BindRecord(record),
            ErrorSchema error => BindError(error),
            ProtocolSchema protocol => BindProtocol(protocol),
            VariantSchema variant => BindVariant(variant),
            PrimitiveSchema or EnumSchema or FixedSchema => schema,
            _ => throw new InvalidOperationException($"Unhandled Avro schema type: {schema.GetType()}"),
        };

        _boundSchemas.Add(schema, bound);
        return bound;
    }

    private AvroSchema BindReference(AvroSchemaReference reference)
    {
        if (!_references.TryGetValue(reference.SchemaName, out var csharpName) ||
            csharpName is not { } resolved ||
            reference.CSharpName == resolved)
        {
            return reference;
        }

        return reference with { CSharpName = resolved };
    }

    private AvroSchema BindArray(ArraySchema array)
    {
        var itemSchema = Bind(array.ItemSchema);
        return ReferenceEquals(itemSchema, array.ItemSchema)
            ? array
            : new ArraySchema(itemSchema, array.Documentation, array.Properties);
    }

    private AvroSchema BindMap(MapSchema map)
    {
        var valueSchema = Bind(map.ValueSchema);
        return ReferenceEquals(valueSchema, map.ValueSchema)
            ? map
            : new MapSchema(valueSchema, map.Documentation, map.Properties);
    }

    private AvroSchema BindUnion(UnionSchema union)
    {
        var schemas = BindItems(union.Schemas, Bind, out var schemasChanged);
        if (union.UnderlyingSchema is VariantSchema variant)
        {
            var boundVariant = (VariantSchema)Bind(variant);
            if (!schemasChanged && ReferenceEquals(boundVariant, variant))
                return union;

            return UnionSchema.Create(schemas, _options.UseNullableReferenceTypes).WithVariant(boundVariant);
        }

        return schemasChanged
            ? UnionSchema.Create(schemas, _options.UseNullableReferenceTypes)
            : union;
    }

    private AvroSchema BindLogical(LogicalSchema logical)
    {
        var underlyingSchema = Bind(logical.UnderlyingSchema);
        return ReferenceEquals(underlyingSchema, logical.UnderlyingSchema)
            ? logical
            : LogicalSchema.Create(logical.SchemaName.Name, underlyingSchema, _options.GenerationTarget);
    }

    private AvroSchema BindRecord(RecordSchema record)
    {
        var fields = BindItems(record.Fields, BindField, out var fieldsChanged);
        return fieldsChanged ? record with { Fields = fields } : record;
    }

    private AvroSchema BindError(ErrorSchema error)
    {
        var fields = BindItems(error.Fields, BindField, out var changed);
        return changed ? error with { Fields = fields } : error;
    }

    private AvroSchema BindProtocol(ProtocolSchema protocol)
    {
        var types = BindItems(protocol.Types, NamedSchema, out var typesChanged);
        var messages = BindItems(protocol.Messages, BindMessage, out var messagesChanged);
        return typesChanged || messagesChanged
            ? protocol with
            {
                Types = types,
                Messages = messages
            }
            : protocol;

        NamedSchema NamedSchema(NamedSchema schema) => (NamedSchema)Bind(schema);
    }

    private AvroSchema BindVariant(VariantSchema variant)
    {
        var derivedSchemas = BindItems(variant.DerivedSchemas, Bind, out var changed);
        return changed ? new VariantSchema(variant.SchemaName, derivedSchemas) : variant;
    }

    private Field BindField(Field field)
    {
        var type = Bind(field.Type);
        if (ReferenceEquals(type, field.Type))
            return field;

        return field with
        {
            Type = type,
            UnderlyingType = GetUnderlyingType(type),
        };
    }

    private ProtocolMessage BindMessage(ProtocolMessage message)
    {
        var requestParameters = BindItems(message.RequestParameters, BindRequestParameter, out var requestChanged);
        var response = BindResponse(message.Response);
        var errors = BindItems(message.Errors, Bind, out var errorsChanged);
        return requestChanged || !ReferenceEquals(response, message.Response) || errorsChanged
            ? message with
            {
                RequestParameters = requestParameters,
                Response = response,
                Errors = errors,
            }
            : message;
    }

    private ProtocolRequestParameter BindRequestParameter(ProtocolRequestParameter parameter)
    {
        var type = Bind(parameter.Type);
        return ReferenceEquals(type, parameter.Type)
            ? parameter
            : parameter with
            {
                Type = type,
                UnderlyingType = GetUnderlyingType(type),
            };
    }

    private ProtocolResponse BindResponse(ProtocolResponse response)
    {
        var type = Bind(response.Type);
        return ReferenceEquals(type, response.Type)
            ? response
            : new ProtocolResponse(type, GetUnderlyingType(type));
    }

    private static AvroSchema GetUnderlyingType(AvroSchema schema) =>
        schema is UnionSchema union ? union.UnderlyingSchema : schema;

    private static ImmutableArray<T> BindItems<T>(ImmutableArray<T> items, Func<T, T> bind, out bool changed) where T : class
    {
        ImmutableArray<T>.Builder? boundItems = null;
        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];
            var bound = bind(item);
            if (!ReferenceEquals(bound, item))
            {
                if (boundItems is null)
                {
                    boundItems = ImmutableArray.CreateBuilder<T>(items.Length);
                    boundItems.AddRange(items.AsSpan(0, i));
                }
            }
            boundItems?.Add(bound);
        }
        changed = boundItems is not null;
        return boundItems?.MoveToImmutable() ?? items;
    }
}
