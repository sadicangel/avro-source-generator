using System.Collections;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Exceptions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Registry;

public readonly record struct SchemaRegistry(SchemaRegistryOptions Options) : IReadOnlyCollection<TopLevelSchema>
{
    private readonly Dictionary<SchemaName, TopLevelSchema> _storedSchemas = [];
    private readonly Dictionary<SchemaName, TopLevelSchema> _stagedSchemas = [];
    private readonly List<SchemaName> _recursionStack = [];

    public int Count => _storedSchemas.Count;

    public IEnumerator<TopLevelSchema> GetEnumerator() => _storedSchemas.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IReadOnlyDictionary<SchemaName, TopLevelSchema> Schemas => _storedSchemas;

    public void Register(TopLevelSchema schema)
    {
        if (TryRegister(schema))
        {
            return;
        }

        if (Options.DuplicateResolution == DuplicateResolution.Ignore)
        {
            return;
        }

        throw new DuplicateSchemaException(schema);

        // TODO: We should probably add 'Replace' resolution as well.
    }

    public bool TryRegister(TopLevelSchema schema)
    {
        if (_storedSchemas.ContainsKey(schema.SchemaName)) return false;
        if (_stagedSchemas.ContainsKey(schema.SchemaName)) return false;
        _stagedSchemas[schema.SchemaName] = schema;
        return true;
    }

    public AvroSchema? Find(SchemaName schemaName)
    {
        // TODO: Isn't this an issue for types that have names that collide with primitive types? Do we need to support that?
        switch (schemaName.Name)
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


        if (_stagedSchemas.TryGetValue(schemaName, out var stagedSchemas) && stagedSchemas is NamedSchema)
            return new AvroSchemaReference(stagedSchemas.SchemaName);

        var index = _recursionStack.FindLastIndex(existing => schemaName == existing);
        if (index >= 0)
            return new AvroSchemaReference(_recursionStack[index]);

        return null;
    }

    public RegisterScope EnterRegisterScope()
    {
        if (_stagedSchemas.Count > 0)
        {
            throw new InvalidOperationException("Cannot enter a new register scope while another is active.");
        }

        return new RegisterScope(_storedSchemas, _stagedSchemas);
    }

    public RecursionScope EnterRecursionScope(SchemaName schemaName) => new(_recursionStack, schemaName);

    public readonly struct RegisterScope : IDisposable
    {
        private readonly Dictionary<SchemaName, TopLevelSchema> _storedSchemas;
        private readonly Dictionary<SchemaName, TopLevelSchema> _stagedSchemas;

        internal RegisterScope(
            Dictionary<SchemaName, TopLevelSchema> storedSchemas,
            Dictionary<SchemaName, TopLevelSchema> stagedSchemas)
        {
            _storedSchemas = storedSchemas;
            _stagedSchemas = stagedSchemas;
        }

        public void Dispose()
        {
            foreach (var entry in _stagedSchemas)
            {
                _storedSchemas[entry.Key] = entry.Value;
            }

            _stagedSchemas.Clear();
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
