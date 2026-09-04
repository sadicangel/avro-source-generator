using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Inputs;

internal sealed class BoundAvroFile : IEquatable<BoundAvroFile>
{
    private readonly LinkedAvroFile _linkedFile;

    private BoundAvroFile(
        LinkedAvroFile linkedFile,
        AvroSchema? rootSchema,
        ImmutableArray<TopLevelSchema> declarations)
    {
        _linkedFile = linkedFile;
        RootSchema = rootSchema;
        Declarations = declarations;
    }

    public AvroFile File => _linkedFile.File;

    public AvroSchema? RootSchema { get; }

    public ImmutableArray<TopLevelSchema> Declarations { get; }

    public IReadOnlyDictionary<SchemaName, CSharpName?> References => _linkedFile.References;

    public IReadOnlyDictionary<SchemaName, ImmutableArray<SchemaName>> Dependencies => File.Dependencies;

    public bool Equals(BoundAvroFile? other) =>
        ReferenceEquals(this, other) ||
        other is not null && _linkedFile.Equals(other._linkedFile);

    public override bool Equals(object? obj) => obj is BoundAvroFile other && Equals(other);

    public override int GetHashCode() => _linkedFile.GetHashCode();

    public static BoundAvroFile FromInput(LinkedAvroFile linkedFile, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var file = linkedFile.File;
        // TODO: Can a file be valid and have a null root schema? If not, we can remove the null check for RootSchema.
        if (!file.IsValid || file.RootSchema is null)
            return new BoundAvroFile(linkedFile, null, []);

        var binder = new SchemaBinder(linkedFile, cancellationToken);
        var rootSchema = binder.Bind(file.RootSchema);
        var declarations = ImmutableArray.CreateBuilder<TopLevelSchema>(file.Declarations.Length);
        foreach (var declaration in file.Declarations)
            declarations.Add((TopLevelSchema)binder.Bind(declaration));

        return new BoundAvroFile(linkedFile, rootSchema, declarations.MoveToImmutable());
    }

    private sealed class SchemaBinder(LinkedAvroFile linkedFile, CancellationToken cancellationToken)
    {
        private readonly CancellationToken _cancellationToken = cancellationToken;
        private readonly AvroParseOptions _options = linkedFile.File.ParseOptions;
        private readonly IReadOnlyDictionary<SchemaName, CSharpName?> _references = linkedFile.References;
        private readonly Dictionary<AvroSchema, AvroSchema> _boundSchemas = new(SchemaReferenceComparer.Instance);

        public AvroSchema Bind(AvroSchema schema)
        {
            _cancellationToken.ThrowIfCancellationRequested();
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
            var schemas = BindSchemas(union.Schemas, out var schemasChanged);
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
                : LogicalSchema.Create(logical.SchemaName.Name, underlyingSchema, _options.TargetProfile);
        }

        private AvroSchema BindRecord(RecordSchema record)
        {
            var fields = BindFields(record.Fields, out var fieldsChanged);
            return fieldsChanged ? record with { Fields = fields } : record;
        }

        private AvroSchema BindError(ErrorSchema error)
        {
            var fields = BindFields(error.Fields, out var changed);
            return changed ? error with { Fields = fields } : error;
        }

        private AvroSchema BindProtocol(ProtocolSchema protocol)
        {
            var types = BindNamedSchemas(protocol.Types, out var typesChanged);
            var messages = BindMessages(protocol.Messages, out var messagesChanged);
            return typesChanged || messagesChanged
                ? protocol with { Types = types, Messages = messages }
                : protocol;
        }

        private AvroSchema BindVariant(VariantSchema variant)
        {
            var derivedSchemas = BindSchemas(variant.DerivedSchemas, out var changed);
            return changed ? new VariantSchema(variant.SchemaName, derivedSchemas) : variant;
        }

        private ImmutableArray<Field> BindFields(ImmutableArray<Field> fields, out bool changed) =>
            BindItems(fields, BindField, out changed);

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

        private ImmutableArray<NamedSchema> BindNamedSchemas(
            ImmutableArray<NamedSchema> schemas,
            out bool changed) =>
            BindItems(schemas, schema => (NamedSchema)Bind(schema), out changed);

        private ImmutableArray<ProtocolMessage> BindMessages(
            ImmutableArray<ProtocolMessage> messages,
            out bool changed) =>
            BindItems(messages, BindMessage, out changed);

        private ProtocolMessage BindMessage(ProtocolMessage message)
        {
            var requestParameters = BindRequestParameters(message.RequestParameters, out var requestChanged);
            var response = BindResponse(message.Response);
            var errors = BindSchemas(message.Errors, out var errorsChanged);
            return requestChanged || !ReferenceEquals(response, message.Response) || errorsChanged
                ? message with
                {
                    RequestParameters = requestParameters,
                    Response = response,
                    Errors = errors,
                }
                : message;
        }

        private ImmutableArray<ProtocolRequestParameter> BindRequestParameters(
            ImmutableArray<ProtocolRequestParameter> parameters,
            out bool changed) =>
            BindItems(parameters, BindRequestParameter, out changed);

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
                : response with
                {
                    Type = type,
                    UnderlyingType = GetUnderlyingType(type),
                };
        }

        private ImmutableArray<AvroSchema> BindSchemas(
            ImmutableArray<AvroSchema> schemas,
            out bool changed) =>
            BindItems(schemas, Bind, out changed);

        private static AvroSchema GetUnderlyingType(AvroSchema schema) =>
            schema is UnionSchema union ? union.UnderlyingSchema : schema;

        private ImmutableArray<T> BindItems<T>(
            ImmutableArray<T> items,
            Func<T, T> bind,
            out bool changed)
            where T : class
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

    private sealed class SchemaReferenceComparer : IEqualityComparer<AvroSchema>
    {
        public static readonly SchemaReferenceComparer Instance = new();

        public bool Equals(AvroSchema? x, AvroSchema? y) => ReferenceEquals(x, y);

        public int GetHashCode(AvroSchema obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
