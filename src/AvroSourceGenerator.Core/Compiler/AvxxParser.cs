using System.Collections.Frozen;
using System.Collections.Immutable;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Compiler;

public abstract class AvxxParser(AvroParseOptions options)
{
    public static AvroFile Parse(SourceText sourceText, AvroParseOptions parseOptions, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!sourceText.Path.TryGetSourceType(out var sourceType))
            return AvroFile.Invalid(sourceText, AvroDiagnostic.UnsupportedSourceType(SourceSpan.FromSourceText(sourceText)), parseOptions);

        if (string.IsNullOrWhiteSpace(sourceText.Text))
        {
            return AvroFile.Invalid(
                sourceText,
                AvroDiagnostic.EmptySource(SourceSpan.FromSourceText(sourceText)),
                parseOptions);
        }

        return sourceType switch
        {
            SourceType.Avdl => AvdlParser.ParseFile(sourceText, parseOptions, cancellationToken),
            SourceType.Avpr => AvprParser.ParseFile(sourceText, parseOptions, cancellationToken),
            _ => AvscParser.ParseFile(sourceText, parseOptions, cancellationToken)
        };
    }

    protected List<TopLevelSchema> Declarations => field ??= [];
    private Dictionary<SchemaName, int> DeclarationIndexes => field ??= [];
    protected HashSet<SchemaName> References => field ??= [];
    private Dictionary<SchemaName, HashSet<SchemaName>> Dependencies => field ??= [];
    private List<SchemaName> RecursionStack => field ??= [];

    // Provenance belongs to occurrences in a file, never to semantic schema equality.
    protected List<SourceSpan> DeclarationSpans => field ??= [];
    protected Dictionary<SchemaName, List<SourceSpan>> ReferenceSpans => field ??= [];

    protected AvroParseOptions Options { get; } = options;

    protected readonly List<AvroDiagnostic> Diagnostics = [];

    protected void Declare(TopLevelSchema schema, SourceSpan sourceSpan)
    {
        DeclarationSpans.Add(sourceSpan);
        DeclarationIndexes[schema.SchemaName] = Declarations.Count;
        Declarations.Add(schema);

        if (RecursionStack.Count == 0)
            return;

        var current = RecursionStack[^1];
        if (current != schema.SchemaName)
        {
            AddDependency(current, schema.SchemaName);
        }
        else if (RecursionStack.Count > 1)
        {
            AddDependency(RecursionStack[^2], schema.SchemaName);
        }
    }

    protected void Replace(TopLevelSchema original, TopLevelSchema replacement)
    {
        if (!DeclarationIndexes.TryGetValue(original.SchemaName, out var index))
            throw new InvalidOperationException($"Declaration '{original.SchemaName}' was not registered.");

        // The name index points at the latest declaration. Keep earlier duplicates
        // so compilation can report them at their original source locations.
        if (!ReferenceEquals(Declarations[index], original))
        {
            index = Declarations.FindIndex(schema => ReferenceEquals(schema, original));
            if (index < 0)
                throw new InvalidOperationException($"Declaration '{original.SchemaName}' was not registered.");
        }

        Declarations[index] = replacement;
    }

    protected AvroSchema Reference(SchemaName schemaName, string? containingNamespace, SourceSpan sourceSpan)
    {
        switch (schemaName.FullName)
        {
            case AvroTypeNames.Null: return AvroSchema.Null;
            case AvroTypeNames.Boolean: return AvroSchema.Boolean;
            case AvroTypeNames.Int: return AvroSchema.Int;
            case AvroTypeNames.Long: return AvroSchema.Long;
            case AvroTypeNames.Float: return AvroSchema.Float;
            case AvroTypeNames.Double: return AvroSchema.Double;
            case AvroTypeNames.Bytes: return AvroSchema.Bytes;
            case AvroTypeNames.String: return AvroSchema.String;
        }

        schemaName = schemaName.ResolveIn(containingNamespace);
        if (!sourceSpan.IsNone)
        {
            if (!ReferenceSpans.TryGetValue(schemaName, out var spans))
                ReferenceSpans[schemaName] = spans = [];
            spans.Add(sourceSpan);
        }
        if (RecursionStack is [.., var containingSchema])
            AddDependency(containingSchema, schemaName);

        if (DeclarationIndexes.TryGetValue(schemaName, out var index))
            return new AvroSchemaReference(schemaName, Declarations[index].CSharpName);

        if (RecursionStack.Contains(schemaName))
            return new AvroSchemaReference(schemaName);

        References.Add(schemaName);
        return new AvroSchemaReference(schemaName);
    }

    protected AvroSchema ResolveFieldType(
        AvroSchema fieldType,
        FieldName fieldName,
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
                    var schemaName = VariantSchema.GetSchemaName(containingSchemaName, fieldName);
                    var csharpName = CSharpName.FromSchemaName(schemaName);
                    var derivedSchemas = ImmutableArray.CreateBuilder<AvroSchema>(union.Schemas.Length);
                    foreach (var schema in union.Schemas)
                    {
                        if (schema is NamedSchema { Type: not SchemaType.Enum } named)
                        {
                            named = named with { InheritsFrom = csharpName };
                            Replace((TopLevelSchema)schema, named);
                            derivedSchemas.Add(named);
                        }
                        else
                        {
                            derivedSchemas.Add(schema);
                        }
                    }
                    var variant = new VariantSchema(schemaName, csharpName, derivedSchemas.DrainToImmutable());
                    Declare(variant, SourceSpan.None);

                    remarks = variant.Documentation;
                    union = union with
                    {
                        CSharpName = union.CSharpName.HasNullableAnnotation ? variant.CSharpName.WithNullableAnnotation() : variant.CSharpName,
                        Schemas = variant.DerivedSchemas,
                        UnderlyingSchema = variant
                    };
                }

                underlyingType = union.UnderlyingSchema;
                return union;

            case FixedSchema fixedSchema when Options.GenerationTarget is not GenerationTarget.Apache:
                remarks = fixedSchema.Documentation;
                return fieldType;

            default:
                return fieldType;
        }
    }

    protected bool IsInRecursionScope(SchemaName schemaName) => RecursionStack.Contains(schemaName);

    protected RecursionScope EnterRecursionScope(SchemaName schemaName) => new RecursionScope(RecursionStack, schemaName);

    protected void Report(AvroDiagnostic diagnostic) => Diagnostics.Add(diagnostic);

    protected DiagnosticTracker TrackDiagnostics() => new DiagnosticTracker(Diagnostics);

    protected readonly struct DiagnosticTracker(IReadOnlyList<AvroDiagnostic> diagnostics)
    {
        private readonly int _initialCount = diagnostics.Count;

        public bool HasNewDiagnostics => diagnostics.Count != _initialCount;
    }

    protected ImmutableArray<SchemaName> GetReferences() => [.. References.OrderBy(static reference => reference.FullName, StringComparer.Ordinal)];

    protected FrozenDictionary<SchemaName, ImmutableArray<SourceSpan>> GetReferenceSpans()
    {
        var references = new Dictionary<SchemaName, ImmutableArray<SourceSpan>>(ReferenceSpans.Count);
        foreach (var pair in ReferenceSpans)
            references.Add(pair.Key, [.. pair.Value.OrderBy(static span => span.Offset)]);
        return references.ToFrozenDictionary();
    }

    private void AddDependency(SchemaName schema, SchemaName dependsOn)
    {
        if (!Dependencies.TryGetValue(schema, out var dependencies))
            Dependencies.Add(schema, dependencies = []);
        dependencies.Add(dependsOn);
    }

    protected FrozenDictionary<SchemaName, ImmutableArray<SchemaName>> GetDependencies()
    {
        var dependencies = new Dictionary<SchemaName, ImmutableArray<SchemaName>>(Dependencies.Count);
        foreach (var dependency in Dependencies)
        {
            dependencies.Add(
                dependency.Key,
                [.. dependency.Value.OrderBy(static name => name.FullName, StringComparer.Ordinal)]);
        }
        return dependencies.ToFrozenDictionary();
    }

    protected readonly ref struct RecursionScope
    {
        private readonly List<SchemaName> _recursionStack;
        private readonly SchemaName _schemaName;

        public RecursionScope(List<SchemaName> recursionStack, SchemaName schemaName)
        {
            _recursionStack = recursionStack;
            _schemaName = schemaName;

            if (_recursionStack.Contains(schemaName))
                throw new InvalidOperationException($"Recursive schema definition detected for schema '{schemaName}'.");

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
