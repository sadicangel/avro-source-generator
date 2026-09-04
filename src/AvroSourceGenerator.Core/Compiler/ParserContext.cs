using System.Collections.Immutable;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Exceptions;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Compiler;

internal readonly struct ParserContext(AvroParseOptions options)
{
    private readonly List<TopLevelSchema> _declarations = [];
    private readonly HashSet<SchemaName> _references = [];
    private readonly Dictionary<SchemaName, HashSet<SchemaName>> _dependencies = [];
    private readonly List<SchemaName> _recursionStack = [];

    public AvroParseOptions Options { get; } = options;

    public void Declare(TopLevelSchema schema)
    {
        _declarations.Add(schema);

        if (_recursionStack.Count == 0)
            return;

        var current = _recursionStack[^1];
        if (current != schema.SchemaName)
        {
            AddDependency(current, schema.SchemaName);
        }
        else if (_recursionStack.Count > 1)
        {
            AddDependency(_recursionStack[^2], schema.SchemaName);
        }
    }

    public AvroSchema Reference(SchemaName schemaName, string? containingNamespace)
    {
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

        schemaName = schemaName.ResolveIn(containingNamespace);
        if (_recursionStack is [.., var containingSchema])
            AddDependency(containingSchema, schemaName);

        for (var index = _declarations.Count - 1; index >= 0; index--)
        {
            var declaration = _declarations[index];
            if (declaration.SchemaName == schemaName)
                return new AvroSchemaReference(schemaName, declaration.CSharpName);
        }

        if (_recursionStack.Contains(schemaName))
            return new AvroSchemaReference(schemaName);

        _references.Add(schemaName);
        return new AvroSchemaReference(schemaName);
    }

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
                    var variantName = VariantSchema.GetSchemaName(containingSchemaName, fieldName);
                    var inheritedSchemas = union.Schemas
                        .Select(schema => schema is RecordSchema record
                            ? record with { InheritsFrom = CSharpName.FromSchemaName(variantName) }
                            : schema)
                        .ToImmutableArray();
                    ReplaceDeclarations(union.Schemas, inheritedSchemas);

                    var variant = new VariantSchema(variantName, inheritedSchemas);
                    Declare(variant);

                    remarks = variant.Documentation;
                    union = union.WithVariant(variant);
                }

                underlyingType = union.UnderlyingSchema;
                return union;

            case FixedSchema fixedSchema when Options.TargetProfile is not TargetProfile.Apache:
                remarks = fixedSchema.Documentation;
                return fieldType;

            default:
                return fieldType;
        }
    }

    public RecursionScope EnterRecursionScope(SchemaName schemaName) => new(_recursionStack, schemaName);

    public ParseResult Complete(AvroSchema root, ImmutableArray<string> imports = default) => new(
        root,
        [.. _declarations],
        [.. _references.OrderBy(static reference => reference.FullName, StringComparer.Ordinal)],
        GetDependencies(),
        imports.IsDefault ? [] : imports);

    private void AddDependency(SchemaName schema, SchemaName dependsOn)
    {
        if (!_dependencies.TryGetValue(schema, out var dependencies))
            _dependencies.Add(schema, dependencies = []);
        dependencies.Add(dependsOn);
    }

    private Dictionary<SchemaName, ImmutableArray<SchemaName>> GetDependencies()
    {
        var dependencies = new Dictionary<SchemaName, ImmutableArray<SchemaName>>(_dependencies.Count);
        foreach (var dependency in _dependencies)
        {
            dependencies.Add(
                dependency.Key,
                [.. dependency.Value.OrderBy(static name => name.FullName, StringComparer.Ordinal)]);
        }
        return dependencies;
    }

    private void ReplaceDeclarations(
        ImmutableArray<AvroSchema> schemas,
        ImmutableArray<AvroSchema> replacements)
    {
        for (var schemaIndex = 0; schemaIndex < schemas.Length; schemaIndex++)
        {
            if (ReferenceEquals(schemas[schemaIndex], replacements[schemaIndex]))
                continue;

            for (var declarationIndex = 0; declarationIndex < _declarations.Count; declarationIndex++)
            {
                if (ReferenceEquals(_declarations[declarationIndex], schemas[schemaIndex]))
                {
                    _declarations[declarationIndex] = (TopLevelSchema)replacements[schemaIndex];
                    break;
                }
            }
        }
    }

    internal readonly ref struct RecursionScope
    {
        private readonly List<SchemaName> _recursionStack;
        private readonly SchemaName _schemaName;

        public RecursionScope(List<SchemaName> recursionStack, SchemaName schemaName)
        {
            _recursionStack = recursionStack;
            _schemaName = schemaName;

            if (_recursionStack.Contains(schemaName))
                throw new InvalidSchemaException($"Recursive schema definition detected for schema '{schemaName}'.");

            _recursionStack.Add(schemaName);
        }

        public void Dispose()
        {
            if (_recursionStack is not [.., var popped] || popped != _schemaName)
                throw new InvalidOperationException("Recursion stack corrupted.");

            _recursionStack.RemoveAt(_recursionStack.Count - 1);
        }
    }
}
