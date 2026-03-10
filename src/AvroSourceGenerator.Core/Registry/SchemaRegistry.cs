using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Json;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Exceptions;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

public readonly record struct SchemaRegistryOptions(TargetProfile TargetProfile, DuplicateResolution DuplicateResolution, bool UseNullableReferenceTypes)
{
    public static readonly SchemaRegistryOptions Default = new(TargetProfile.Modern, DuplicateResolution.Error, true);
}

[StructLayout(LayoutKind.Auto)]
[SuppressMessage("ReSharper", "UsageOfDefaultStructEquality")]
public readonly partial struct SchemaRegistry(SchemaRegistryOptions options) : IReadOnlyCollection<TopLevelSchema>
{
    private readonly Dictionary<SchemaName, TopLevelSchema> _schemas = [];
    private readonly List<SchemaName> _recursionStack = [];

    public int Count => _schemas.Count;

    public IEnumerator<TopLevelSchema> GetEnumerator() => _schemas.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IReadOnlyDictionary<SchemaName, TopLevelSchema> Schemas => _schemas;

    public void Register(JsonElement schema)
    {
        _ = Schema(schema, containingNamespace: null);

        // TODO: We need to check that the returned schema contains at least a named schema.
        if (Count == 0)
        {
            throw new InvalidSchemaException($"At least a named schema must be present in schema: {schema.GetRawText()}");
        }
    }

    private void Register(TopLevelSchema schema)
    {
        if (TryRegister(schema))
        {
            return;
        }

        if (options.DuplicateResolution == DuplicateResolution.Ignore)
        {
            return;
        }

        // TODO: Needs to be its own exception type so we can report a proper diagnostic.
        throw new DuplicateSchemaException(schema);

        // TODO: We should probably add 'Replace' resolution as well.
    }

    private bool TryRegister(TopLevelSchema schema)
    {
        if (_schemas.ContainsKey(schema.SchemaName)) return false;
        _schemas[schema.SchemaName] = schema;
        return true;
    }

    private AvroSchema? FindByName(string name, string? containingNamespace)
    {
        // TODO: Isn't this an issue for types that have names that collide with primitive types? Do we need to support that?
        switch (name)
        {
            case AvroTypeNames.Null: return AvroSchema.Object;
            case AvroTypeNames.Boolean: return AvroSchema.Boolean;
            case AvroTypeNames.Int: return AvroSchema.Int;
            case AvroTypeNames.Long: return AvroSchema.Long;
            case AvroTypeNames.Float: return AvroSchema.Float;
            case AvroTypeNames.Double: return AvroSchema.Double;
            case AvroTypeNames.Bytes: return AvroSchema.Bytes;
            case AvroTypeNames.String: return AvroSchema.String;
        }

        var schemaName = name.ToSchemaName(containingNamespace);
        if (_schemas.TryGetValue(schemaName, out var topLevelSchema) && topLevelSchema is NamedSchema)
            return new AvroSchemaReference(topLevelSchema.SchemaName);

        var index = _recursionStack.FindLastIndex(existing => schemaName == existing);
        if (index >= 0)
            return new AvroSchemaReference(_recursionStack[index]);

        return null;
    }

    private RecursionScope EnterRecursionScope(SchemaName schemaName) => new(_recursionStack, schemaName);

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

        return FindByName(type, containingNamespace) ?? throw new InvalidSchemaException($"Unknown schema '{type}'");
    }

    private AvroSchema Complex(JsonElement schema, string? containingNamespace)
    {
        if (schema.TryGetProperty(AvroJsonKeys.Protocol, out _))
        {
            return Protocol(schema, containingNamespace);
        }

        var underlyingSchema = UnderlyingSchema(schema, containingNamespace);

        return schema.TryGetProperty(AvroJsonKeys.LogicalType, out _) ? Logical(schema, underlyingSchema) : underlyingSchema;
    }

    private AvroSchema UnderlyingSchema(JsonElement schema, string? containingNamespace)
    {
        var type = schema.GetSchemaType();
        switch (type)
        {
            case AvroTypeNames.Array: return Array(schema, containingNamespace);
            case AvroTypeNames.Map: return Map(schema, containingNamespace);
            case AvroTypeNames.Enum: return Enum(schema, containingNamespace);
            case AvroTypeNames.Record: return Record(schema, containingNamespace);
            case AvroTypeNames.Error: return Error(schema, containingNamespace);
            case AvroTypeNames.Fixed: return Fixed(schema, containingNamespace);
        }

        var wellKnown = FindByName(type, containingNamespace)
            ?? throw new InvalidSchemaException($"Unknown schema type '{type}' in {schema.GetRawText()}");

        if (wellKnown is PrimitiveSchema primitive)
        {
            return primitive with
            {
                Documentation = schema.GetDocumentation(),
                Properties = schema.GetSchemaProperties(),
            };
        }

        // TODO: Should we add/merge properties for other schema types?
        return wellKnown;
    }

    // TODO: This should probably be in AvroSchema hierarchy instead of being here.
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
            "object" or "bool" or "int" or "long" => value.GetRawText(),
            "float" => $"{value.GetRawText()}f",
            "double" => value.GetRawText(),
            "byte[]" => $"[{string.Join(", ", value.GetBytesFromBase64().Select(bytes => $"0x{bytes:X2}"))}]",
            "string" => value.GetRawText(),
            _ when type.Type is SchemaType.Enum => $"{type}.{value.GetString()}",

            // TODO: Do we need to handle complex types? Should they be supported?
            _ => null,
        };
    }

    private readonly ref struct RecursionScope : IDisposable
    {
        private readonly List<SchemaName> _recursionStack;
        private readonly SchemaName _schemaName;

        public RecursionScope(List<SchemaName> recursionStack, SchemaName schemaName)
        {
            _recursionStack = recursionStack;
            _schemaName = schemaName;

            if (_recursionStack.Contains(schemaName))
            {
                throw new InvalidSchemaException($"Recursive schema definition detected for schema '{schemaName}'.");
            }

            _recursionStack.Add(schemaName);
        }

        public void Dispose()
        {
            if (_recursionStack is not [.., var popped] || popped != _schemaName)
            {
                throw new InvalidOperationException("Recursion stack corrupted.");
            }

            _recursionStack.RemoveAt(_recursionStack.Count - 1);
        }
    }
}
