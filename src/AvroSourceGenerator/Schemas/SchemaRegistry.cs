using System.Collections;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using AvroSourceGenerator.Schemas.Extensions;

namespace AvroSourceGenerator.Schemas;

internal readonly struct SchemaRegistry(bool useNullableReferenceTypes) : IReadOnlyCollection<TopLevelSchema>
{
    private readonly Dictionary<SchemaName, TopLevelSchema> _schemas = [];
    private static readonly HashSet<string> s_reservedProperties = ["type", "name", "namespace", "fields", "items", "size", "symbols", "values", "aliases", "order", "doc", "default", "logicalType"];


    public int Count => _schemas.Count;

    public IEnumerator<TopLevelSchema> GetEnumerator() => _schemas.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static SchemaRegistry Register(JsonElement schema, bool useNullableReferenceTypes)
    {
        var registry = new SchemaRegistry(useNullableReferenceTypes);

        _ = registry.Schema(schema, containingNamespace: null);

        if (registry.Count == 0)
        {
            throw new InvalidSchemaException($"Atleast a named schema must be present in schema: {schema.GetRawText()}");
        }

        return registry;
    }

    private AvroSchema Schema(JsonElement schema, string? containingNamespace)
    {
        return schema.ValueKind switch
        {
            JsonValueKind.String => WellKnown(schema, containingNamespace),
            JsonValueKind.Object => Complex(schema, containingNamespace),
            JsonValueKind.Array => Union(schema, containingNamespace),
            _ => throw new InvalidSchemaException($"Invalid schema: {schema.GetRawText()}")
        };
    }

    private AvroSchema WellKnown(JsonElement schema, string? containingNamespace)
    {
        var type = schema.GetString() ?? throw new InvalidOperationException($"Unexpected json value '{schema}'. Expected 'string'");

        return type switch
        {
            "null" => AvroSchema.Object,
            "boolean" => AvroSchema.Boolean,
            "int" => AvroSchema.Int,
            "long" => AvroSchema.Long,
            "float" => AvroSchema.Float,
            "double" => AvroSchema.Double,
            "bytes" => AvroSchema.Bytes,
            "string" => AvroSchema.String,
            _ when _schemas.TryGetValue(type.GetRequiredSchemaName(containingNamespace), out var topLevelSchema)
            && topLevelSchema is NamedSchema namedSchema => namedSchema,
            _ => throw new InvalidSchemaException($"Unknown schema '{type}'")
        };
    }

    private AvroSchema Complex(JsonElement schema, string? containingNamespace)
    {
        if (schema.TryGetProperty("logicalType", out var _))
        {
            return Logical(schema, GetProperties(schema), containingNamespace);
        }

        if (schema.TryGetProperty("protocol", out _))
        {
            return Protocol(schema, GetProperties(schema), containingNamespace);
        }

        var type = schema.GetSchemaType();

        return type switch
        {
            "array" => Array(schema, GetProperties(schema), containingNamespace),
            "map" => Map(schema, GetProperties(schema), containingNamespace),
            "enum" => Enum(schema, GetProperties(schema), containingNamespace),
            "record" => Record(schema, GetProperties(schema), containingNamespace),
            "error" => Error(schema, GetProperties(schema), containingNamespace),
            "fixed" => Fixed(schema, GetProperties(schema), containingNamespace),
            _ => NamedSchema(schema, containingNamespace),
        };
    }

    private NamedSchema NamedSchema(JsonElement schema, string? containingNamespace)
    {
        var type = schema.GetSchemaType();

        return type switch
        {
            "enum" => Enum(schema, GetProperties(schema), containingNamespace),
            "record" => Record(schema, GetProperties(schema), containingNamespace),
            "error" => Error(schema, GetProperties(schema), containingNamespace),
            "fixed" => Fixed(schema, GetProperties(schema), containingNamespace),
            _ => throw new InvalidSchemaException($"Unknown schema type '{type}' in {schema.GetRawText()}")
        };
    }

    private ImmutableSortedDictionary<string, JsonElement> GetProperties(JsonElement schema)
    {
        var properties = ImmutableSortedDictionary.CreateBuilder<string, JsonElement>();
        foreach (var property in schema.EnumerateObject())
        {
            if (!s_reservedProperties.Contains(property.Name))
                properties.Add(property.Name, property.Value);
        }
        return properties.ToImmutable();
    }

    private ArraySchema Array(JsonElement schema, ImmutableSortedDictionary<string, JsonElement> properties, string? containingNamespace)
    {
        var itemsSchema = schema.GetRequiredProperty("items");

        var items = Schema(itemsSchema, containingNamespace);

        return new ArraySchema(items, properties);
    }

    private MapSchema Map(JsonElement schema, ImmutableSortedDictionary<string, JsonElement> properties, string? containingNamespace)
    {
        var valuesSchema = schema.GetRequiredProperty("values");

        var values = Schema(valuesSchema, containingNamespace);

        return new MapSchema(values, properties);
    }

    private EnumSchema Enum(JsonElement schema, ImmutableSortedDictionary<string, JsonElement> properties, string? containingNamespace)
    {
        var schemaName = schema.GetRequiredSchemaName(containingNamespace);

        if (_schemas.ContainsKey(schemaName))
            throw new InvalidSchemaException($"Redeclaration of schema '{schemaName}'");

        var documentation = schema.GetDocumentation();
        var aliases = schema.GetAliases();
        var symbols = schema.GetSymbols();
        var @default = schema.GetNullableString("default");

        var enumSchema = new EnumSchema(schema, schemaName, documentation, aliases, symbols, @default, properties);
        _schemas[schemaName] = enumSchema;

        return enumSchema;
    }

    private RecordSchema Record(JsonElement schema, ImmutableSortedDictionary<string, JsonElement> properties, string? containingNamespace)
    {
        var schemaName = schema.GetRequiredSchemaName(containingNamespace);

        if (_schemas.ContainsKey(schemaName))
            throw new InvalidSchemaException($"Redeclaration of schema '{schemaName}'");

        var documentation = schema.GetDocumentation();
        var aliases = schema.GetAliases();
        var fields = Fields(schema, schemaName.Namespace);

        var recordSchema = new RecordSchema(schema, schemaName, documentation, aliases, fields, properties);

        _schemas[schemaName] = recordSchema;

        return recordSchema;
    }

    private ErrorSchema Error(JsonElement schema, ImmutableSortedDictionary<string, JsonElement> properties, string? containingNamespace)
    {
        var schemaName = schema.GetRequiredSchemaName(containingNamespace);

        if (_schemas.ContainsKey(schemaName))
            throw new InvalidSchemaException($"Redeclaration of schema '{schemaName}'");

        var documentation = schema.GetDocumentation();
        var aliases = schema.GetAliases();
        var fields = Fields(schema, schemaName.Namespace);

        var errorSchema = new ErrorSchema(schema, schemaName, documentation, aliases, fields, properties);
        _schemas[schemaName] = errorSchema;

        return errorSchema;
    }

    private ImmutableArray<Field> Fields(JsonElement schema, string? containingNamespace)
    {
        var fields = ImmutableArray.CreateBuilder<Field>();
        foreach (var field in schema.GetRequiredArray("fields"))
            fields.Add(Field(field, containingNamespace));

        return fields.ToImmutable();
    }

    private Field Field(JsonElement field, string? containingNamespace)
    {
        var name = field.GetRequiredString("name").GetValidName();
        var type = Schema(field.GetRequiredProperty("type"), containingNamespace);
        var underlyingType = type;
        var isNullable = false;
        if (type is UnionSchema union)
        {
            isNullable = union.IsNullable;
            underlyingType = union.UnderlyingSchema;
        }

        var documentation = field.GetDocumentation();
        var aliases = field.GetAliases();
        var defaultJson = field.GetNullableProperty("default");
        var @default = GetValue(type, defaultJson);
        var order = field.GetNullableInt32("order");
        var properties = GetProperties(field);
        return new Field(name, type, underlyingType, isNullable, documentation, aliases, defaultJson, @default, order, properties);
    }

    private string? GetValue(AvroSchema type, JsonElement? json)
    {
        if (json is null or { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined })
        {
            return null;
        }

        var value = json.Value;

        // TODO: Actually validate the value so that we don't generate invalid code.
        return type.CSharpName.Name switch
        {
            "object" => value.GetRawText(),
            "bool" => value.GetRawText(),
            "int" => value.GetRawText(),
            "long" => value.GetRawText(),
            "float" => $"{value.GetRawText()}f",
            "double" => value.GetRawText(),
            "byte[]" => $"[{string.Join(", ", value.GetBytesFromBase64().Select(bytes => $"0x{bytes:X2}"))}]",
            "string" => value.GetRawText(),
            _ when _schemas.TryGetValue(type.SchemaName, out var namedSchema) && namedSchema.Type is SchemaType.Enum => $"{type}.{value.GetString()}",

            // TODO: Do we need to handle complex types? Should they be supported?
            _ => null,

        };
    }

    private FixedSchema Fixed(JsonElement schema, ImmutableSortedDictionary<string, JsonElement> properties, string? containingNamespace)
    {
        var schemaName = schema.GetRequiredSchemaName(containingNamespace);

        if (_schemas.ContainsKey(schemaName))
            throw new InvalidSchemaException($"Redeclaration of schema '{schemaName}'");

        var documentation = schema.GetDocumentation();
        var aliases = schema.GetAliases();
        var size = schema.GetFixedSize();

        var fixedSchema = new FixedSchema(schema, schemaName, documentation, aliases, size, properties);
        _schemas[schemaName] = fixedSchema;

        return fixedSchema;
    }

    private AvroSchema Logical(JsonElement schema, ImmutableSortedDictionary<string, JsonElement> properties, string? containingNamespace)
    {
        var logicalType = schema.GetRequiredString("logicalType");

        var underlyingType = schema.GetRequiredString("type");
        var underlyingSchema = underlyingType switch
        {
            "null" => AvroSchema.Object,
            "boolean" => AvroSchema.Boolean,
            "int" => AvroSchema.Int,
            "long" => AvroSchema.Long,
            "float" => AvroSchema.Float,
            "double" => AvroSchema.Double,
            "bytes" => AvroSchema.Bytes,
            "string" => AvroSchema.String,
            "array" => Array(schema, ImmutableSortedDictionary<string, JsonElement>.Empty, containingNamespace),
            "map" => Map(schema, ImmutableSortedDictionary<string, JsonElement>.Empty, containingNamespace),
            "enum" => Enum(schema, ImmutableSortedDictionary<string, JsonElement>.Empty, containingNamespace),
            "record" => Record(schema, ImmutableSortedDictionary<string, JsonElement>.Empty, containingNamespace),
            "error" => Error(schema, ImmutableSortedDictionary<string, JsonElement>.Empty, containingNamespace),
            "fixed" => Fixed(schema, ImmutableSortedDictionary<string, JsonElement>.Empty, containingNamespace),
            _ when _schemas.TryGetValue(
                new SchemaName(JsonElementExtensions.GetValidName(underlyingType),
                containingNamespace), out var topLevelSchema)
            && topLevelSchema is NamedSchema namedSchema => namedSchema,
            _ => throw new InvalidSchemaException($"Unknown schema type '{underlyingType}' in {schema.GetRawText()}")
        };

        return logicalType switch
        {
            "date" => new LogicalSchema(underlyingSchema, new CSharpName("DateTime", "System"), new SchemaName(logicalType), properties),
            "decimal" => new LogicalSchema(underlyingSchema, new CSharpName("AvroDecimal", "Avro"), new SchemaName(logicalType), properties),
            "duration" => new LogicalSchema(underlyingSchema, underlyingSchema.CSharpName, new SchemaName(logicalType), properties),
            "time-micros" => new LogicalSchema(underlyingSchema, new CSharpName("TimeSpan", "System"), new SchemaName(logicalType), properties),
            "time-millis" => new LogicalSchema(underlyingSchema, new CSharpName("TimeSpan", "System"), new SchemaName(logicalType), properties),
            "timestamp-micros" => new LogicalSchema(underlyingSchema, new CSharpName("DateTime", "System"), new SchemaName(logicalType), properties),
            "timestamp-millis" => new LogicalSchema(underlyingSchema, new CSharpName("DateTime", "System"), new SchemaName(logicalType), properties),
            "local-timestamp-micros" => new LogicalSchema(underlyingSchema, new CSharpName("DateTime", "System"), new SchemaName(logicalType), properties),
            "local-timestamp-millis" => new LogicalSchema(underlyingSchema, new CSharpName("DateTime", "System"), new SchemaName(logicalType), properties),
            "uuid" => new LogicalSchema(underlyingSchema, new CSharpName("Guid", "System"), new SchemaName(logicalType), properties),
            // _ => throw new InvalidSchemaException($"Unsupported logical type '{logicalType}' in schema: {schema.GetRawText()}"),
            // TODO: We should report a warning for unsupported logical types, maybe? But always return the underlying schema.
            _ => underlyingSchema,
        };
    }

    private UnionSchema Union(JsonElement schema, string? containingNamespace)
    {
        var builder = ImmutableArray.CreateBuilder<AvroSchema>();
        foreach (var innerSchema in schema.EnumerateArray())
            builder.Add(Schema(innerSchema, containingNamespace));
        var schemas = builder.ToImmutable();

        var union = UnionSchema.FromArray(schemas, useNullableReferenceTypes, containingNamespace);

        if (union.UnderlyingSchema.Type is SchemaType.Abstract)
        {
            _schemas.Add(union.UnderlyingSchema.SchemaName, (TopLevelSchema)union.UnderlyingSchema);

            foreach (var item in union.Schemas)
            {
                ((RecordSchema)item).InheritsFrom = new CSharpName(
                    Name: union.Schemas.Aggregate(new StringBuilder("OneOf"), (acc, val) => acc.Append(val.CSharpName.Name), acc => acc.ToString()),
                    Namespace: containingNamespace);
            }
        }

        return union;
    }

    private ProtocolSchema Protocol(JsonElement schema, ImmutableSortedDictionary<string, JsonElement> properties, string? containingNamespace)
    {
        var schemaName = schema.GetRequiredSchemaName(containingNamespace, propertyName: "protocol");

        if (_schemas.ContainsKey(schemaName))
            throw new InvalidSchemaException($"Redeclaration of schema '{schemaName}'");

        var documentation = schema.GetDocumentation();
        var types = ProtocolTypes(schema.GetRequiredArray("types"), schemaName.Namespace);
        var messages = ProtocolMessages(schema.GetRequiredObject("messages"), schemaName.Namespace);

        var protocolSchema = new ProtocolSchema(schema, schemaName, documentation, types, messages, properties);
        _schemas[schemaName] = protocolSchema;

        return protocolSchema;
    }

    private ImmutableArray<NamedSchema> ProtocolTypes(JsonElement.ArrayEnumerator schemas, string? containingNamespace)
    {
        var types = ImmutableArray.CreateBuilder<NamedSchema>();
        foreach (var type in schemas)
            types.Add(NamedSchema(type, containingNamespace));

        return types.ToImmutable();
    }

    private ImmutableArray<ProtocolMessage> ProtocolMessages(JsonElement.ObjectEnumerator messages, string? containingNamespace)
    {
        var protocolMessages = ImmutableArray.CreateBuilder<ProtocolMessage>();
        foreach (var message in messages)
            protocolMessages.Add(Message(message, containingNamespace));
        return protocolMessages.ToImmutable();
    }

    private ProtocolMessage Message(JsonProperty property, string? containingNamespace)
    {
        var methodName = property.Name.GetValidName();
        var documentation = property.Value.GetDocumentation();
        var requestParameters = ProtocolRequestParameters(property.Value, containingNamespace);
        var response = ProtocolResponse(property.Value.GetRequiredProperty("response"), containingNamespace);
        var errors = ProtocolErrors(property.Value.GetNullableArray("errors"), containingNamespace);
        return new ProtocolMessage(methodName, documentation, requestParameters, response, errors);
    }

    private ImmutableArray<ProtocolRequestParameter> ProtocolRequestParameters(JsonElement schema, string? containingNamespace)
    {
        var fields = ImmutableArray.CreateBuilder<ProtocolRequestParameter>();
        foreach (var parameter in schema.GetRequiredArray("request"))
            fields.Add(ProtocolRequestParameter(parameter, containingNamespace));

        return fields.ToImmutable();
    }

    private ProtocolRequestParameter ProtocolRequestParameter(JsonElement field, string? containingNamespace)
    {
        var name = field.GetRequiredString("name").GetValidName();
        var type = Schema(field.GetRequiredProperty("type"), containingNamespace);
        var underlyingType = type;
        var isNullable = false;
        if (type is UnionSchema union)
        {
            isNullable = union.IsNullable;
            underlyingType = union.UnderlyingSchema;
        }

        var documentation = field.GetDocumentation();
        var defaultJson = field.GetNullableProperty("default");
        var @default = GetValue(type, defaultJson);
        return new ProtocolRequestParameter(name, type, underlyingType, isNullable, documentation, defaultJson, @default);
    }

    private ProtocolResponse ProtocolResponse(JsonElement schema, string? containingNamespace)
    {
        var type = Schema(schema, containingNamespace);
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
        builder.Add(AvroSchema.String);
        if (errors is null)
        {
            return builder.ToImmutable();
        }

        foreach (var error in errors.Value)
        {
            builder.Add(Schema(error, containingNamespace));
        }

        return builder.ToImmutable();
    }
}

abstract record Base()
{

}

sealed record D1 : Base;
sealed record D2 : Base;
