using System.Collections;
using System.Collections.Immutable;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Exceptions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

public readonly record struct SchemaRegistry(SchemaRegistryOptions Options) : IReadOnlyCollection<TopLevelSchema>
{
    private readonly Dictionary<SchemaName, TopLevelSchema> _storedSchemas = [];
    private readonly HashSet<SchemaName> _missingReferences = [];
    private readonly Dictionary<SchemaName, TopLevelSchema> _stagedSchemas = [];
    private readonly HashSet<SchemaName> _stagedReferences = [];
    private readonly List<SchemaName> _recursionStack = [];
    private readonly List<SchemaRegistration> _registrations = [];

    public int Count => _storedSchemas.Count;

    public IEnumerator<TopLevelSchema> GetEnumerator() => _storedSchemas.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IReadOnlyDictionary<SchemaName, TopLevelSchema> Schemas => _storedSchemas;

    internal IReadOnlyList<SchemaRegistration> Registrations => _registrations;

    public void Register(TopLevelSchema schema)
    {
        if (_stagedSchemas.ContainsKey(schema.SchemaName))
        {
            // Duplicate definition in the same file. Always an error.
            _registrations.Add(new SchemaRegistration(schema.SchemaName, SchemaRegistrationKind.RejectedDuplicate));
            throw new DuplicateSchemaException(schema);
        }

        if (_storedSchemas.TryGetValue(schema.SchemaName, out var storedSchema))
        {
            if (Options.DuplicateResolution != DuplicateResolution.Ignore)
            {
                _registrations.Add(new SchemaRegistration(schema.SchemaName, SchemaRegistrationKind.RejectedDuplicate));
                throw new DuplicateSchemaException(schema);
            }

            // TODO: We should probably add 'Replace' resolution as well.

            // Keep the previously registered schema visible inside the current file so later references can still resolve it.
            _registrations.Add(new SchemaRegistration(schema.SchemaName, SchemaRegistrationKind.IgnoredDuplicate));
            schema = storedSchema;
        }
        else
        {
            _registrations.Add(new SchemaRegistration(schema.SchemaName, SchemaRegistrationKind.Registered));
        }

        _stagedSchemas[schema.SchemaName] = schema;
    }

    internal readonly record struct SchemaRegistration(SchemaName SchemaName, SchemaRegistrationKind Kind);

    internal enum SchemaRegistrationKind
    {
        Registered,
        IgnoredDuplicate,
        RejectedDuplicate,
    }

    public void AddReference(SchemaName schemaName) => _stagedReferences.Add(schemaName);

    public AvroSchema ResolveFieldType(
        AvroSchema fieldType,
        string fieldName,
        SchemaName containingSchemaName,
        out AvroSchema underlyingType,
        out string? remarks)
    {
        underlyingType = fieldType;
        remarks = null;

        switch (fieldType)
        {
            case UnionSchema union:
                if (union.SupportsVariant())
                {
                    var variant = new VariantSchema(fieldName, containingSchemaName, union.Schemas);
                    // If a variant with the same name already exists, it has the same set of types, so we can just reuse it.
                    if (!Schemas.ContainsKey(variant.SchemaName))
                        Register(variant);

                    remarks = variant.Documentation;
                    union = union.WithVariant(variant);
                }

                underlyingType = union.UnderlyingSchema;
                return union;

            case FixedSchema @fixed when Options.TargetProfile is not TargetProfile.Apache:
                remarks = @fixed.Documentation;
                return fieldType;

            default:
                return fieldType;
        }
    }

    public ImmutableArray<SchemaName> GetMissingReferences() => [.. _missingReferences];

    public AvroSchema? Find(SchemaName schemaName, string? containingNamespace)
    {
        // TODO: Isn't this an issue for types that have names that collide with primitive types? Do we need to support that?
        switch (schemaName.FullName)
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

        if (_stagedReferences.Contains(schemaName))
            return new AvroSchemaReference(schemaName);

        schemaName = schemaName.ResolveIn(containingNamespace);
        var index = _recursionStack.FindLastIndex(existing => schemaName == existing);
        if (index >= 0)
            return new AvroSchemaReference(_recursionStack[index]);

        if (_stagedSchemas.TryGetValue(schemaName, out var stagedSchema) && stagedSchema is NamedSchema)
            return new AvroSchemaReference(stagedSchema.SchemaName, stagedSchema.CSharpName);

        if (_stagedReferences.Contains(schemaName))
            return new AvroSchemaReference(schemaName);

        return null;
    }

    public AvroSchema? Resolve(SchemaName schemaName)
    {
        // TODO: Isn't this an issue for types that have names that collide with primitive types? Do we need to support that?
        return schemaName.FullName switch
        {
            AvroTypeNames.Null => AvroSchema.Object,
            AvroTypeNames.Boolean => AvroSchema.Boolean,
            AvroTypeNames.Int => AvroSchema.Int,
            AvroTypeNames.Long => AvroSchema.Long,
            AvroTypeNames.Float => AvroSchema.Float,
            AvroTypeNames.Double => AvroSchema.Double,
            AvroTypeNames.Bytes => AvroSchema.Bytes,
            AvroTypeNames.String => AvroSchema.String,
            _ => _stagedSchemas.TryGetValue(schemaName, out var stagedSchema) ? stagedSchema : null
        };
    }

    public RegisterScope EnterRegisterScope() => new(_stagedSchemas, _stagedReferences, _storedSchemas, _missingReferences);

    public RecursionScope EnterRecursionScope(SchemaName schemaName) => new(_recursionStack, schemaName);

    public readonly struct RegisterScope : IDisposable
    {
        private readonly Dictionary<SchemaName, TopLevelSchema> _storedSchemas;
        private readonly Dictionary<SchemaName, TopLevelSchema> _stagedSchemas;
        private readonly HashSet<SchemaName> _missingReferences;
        private readonly HashSet<SchemaName> _stagedReferences;

        internal RegisterScope(
            Dictionary<SchemaName, TopLevelSchema> stagedSchemas,
            HashSet<SchemaName> stagedReferences,
            Dictionary<SchemaName, TopLevelSchema> storedSchemas,
            HashSet<SchemaName> missingReferences)
        {
            if (stagedSchemas.Count + stagedReferences.Count > 0)
            {
                throw new InvalidOperationException("Cannot enter a new register scope while another is active.");
            }

            _storedSchemas = storedSchemas;
            _stagedSchemas = stagedSchemas;
            _missingReferences = missingReferences;
            _stagedReferences = stagedReferences;
        }

        public void Dispose()
        {
            foreach (var entry in _stagedSchemas)
            {
                _storedSchemas[entry.Key] = entry.Value;
                _missingReferences.Remove(entry.Key);
            }

            _stagedSchemas.Clear();

            foreach (var reference in _stagedReferences)
            {
                if (!_storedSchemas.ContainsKey(reference))
                    _missingReferences.Add(reference);
            }

            _stagedReferences.Clear();
        }
    }

    public readonly ref struct RecursionScope : IDisposable
    {
        private readonly List<SchemaName> _recursionStack;
        private readonly SchemaName _schemaName;

        internal RecursionScope(List<SchemaName> recursionStack, SchemaName schemaName)
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
