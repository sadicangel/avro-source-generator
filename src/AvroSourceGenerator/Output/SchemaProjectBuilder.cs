using System.Collections.Immutable;
using AvroSourceGenerator.Protocols;
using AvroSourceGenerator.Registry;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Output;

internal sealed class SchemaProjectBuilder
{
    private readonly List<FileRegistration> _files = [];
    private readonly List<StrictMissingReference> _strictMissingReferences = [];

    public void AddFile(
        string path,
        IReadOnlyList<SchemaRegistry.SchemaRegistration> registrations,
        ImmutableArray<SchemaName> strictMissingReferences)
    {
        _files.Add(new FileRegistration(path, [.. registrations]));
        _strictMissingReferences.AddRange(strictMissingReferences.Select(reference => new StrictMissingReference(path, reference)));
    }

    public SchemaProject Build(in SchemaRegistry schemaRegistry)
    {
        var ownerPaths = new Dictionary<SchemaName, string>();
        var files = ImmutableArray.CreateBuilder<SchemaProjectFile>(_files.Count);
        var duplicates = ImmutableArray.CreateBuilder<DuplicateSchemaDefinition>();

        foreach (var file in _files)
        {
            var exports = ImmutableArray.CreateBuilder<SchemaName>();
            var exportedNames = new HashSet<SchemaName>();

            foreach (var registration in file.Registrations)
            {
                var schemaName = registration.SchemaName;
                if (registration.Kind is SchemaRegistry.SchemaRegistrationKind.Registered)
                {
                    if (exportedNames.Add(schemaName))
                        exports.Add(schemaName);
                    if (!ownerPaths.ContainsKey(schemaName))
                        ownerPaths.Add(schemaName, file.Path);
                    continue;
                }

                var ownerPath = GetOwnerPath(ownerPaths, schemaName);
                duplicates.Add(new DuplicateSchemaDefinition(
                    schemaName,
                    ownerPath,
                    file.Path,
                    registration.Kind is SchemaRegistry.SchemaRegistrationKind.IgnoredDuplicate));
            }

            files.Add(new SchemaProjectFile(file.Path, exports.ToImmutable()));
        }

        var schemas = schemaRegistry
            .Select(schema => new OwnedSchema(
                schema.SchemaName,
                GetOwnerPath(ownerPaths, schema.SchemaName),
                EmitsSource(schema)))
            .ToImmutableArray();

        var dependencies = schemaRegistry
            .SelectMany(GetDependencies)
            .ToImmutableArray();

        var knownSchemas = new HashSet<SchemaName>(schemaRegistry.Schemas.Keys);
        var missingReferences = ImmutableArray.CreateBuilder<MissingSchemaReference>();
        foreach (var missingReference in _strictMissingReferences)
        {
            missingReferences.Add(new MissingSchemaReference(missingReference.SourcePath, Schema: null, missingReference.Reference));
        }

        foreach (var dependency in dependencies)
        {
            if (!knownSchemas.Contains(dependency.DependsOn))
            {
                missingReferences.Add(new MissingSchemaReference(
                    GetOwnerPath(ownerPaths, dependency.Schema),
                    dependency.Schema,
                    dependency.DependsOn));
            }
        }

        return new SchemaProject(
            files.MoveToImmutable(),
            schemas,
            dependencies,
            [.. missingReferences.Distinct()],
            duplicates.ToImmutable());
    }

    private static IEnumerable<SchemaDependency> GetDependencies(TopLevelSchema schema)
    {
        var references = new HashSet<SchemaName>();
        Visit(schema, schema, references);
        return references
            .OrderBy(static reference => reference.FullName, StringComparer.Ordinal)
            .Select(reference => new SchemaDependency(schema.SchemaName, reference));
    }

    private static void Visit(TopLevelSchema root, AvroSchema schema, HashSet<SchemaName> references)
    {
        if (schema is AvroSchemaReference reference)
        {
            references.Add(reference.SchemaName);
            return;
        }

        if (schema is TopLevelSchema topLevel && !ReferenceEquals(root, topLevel))
        {
            references.Add(topLevel.SchemaName);
            return;
        }

        switch (schema)
        {
            case ArraySchema array:
                Visit(root, array.ItemSchema, references);
                break;

            case MapSchema map:
                Visit(root, map.ValueSchema, references);
                break;

            case UnionSchema union:
                foreach (var item in union.Schemas)
                    Visit(root, item, references);
                Visit(root, union.UnderlyingSchema, references);
                break;

            case LogicalSchema logical:
                Visit(root, logical.UnderlyingSchema, references);
                break;

            case RecordSchema record:
                VisitFields(root, record.Fields, references);
                if (record.InheritsFrom is { } baseSchema)
                    Visit(root, baseSchema, references);
                break;

            case ErrorSchema error:
                VisitFields(root, error.Fields, references);
                break;

            case ProtocolSchema protocol:
                foreach (var type in protocol.Types)
                    Visit(root, type, references);
                foreach (var message in protocol.Messages)
                {
                    foreach (var parameter in message.RequestParameters)
                        Visit(root, parameter.Type, references);
                    Visit(root, message.Response.Type, references);
                    foreach (var error in message.Errors)
                        Visit(root, error, references);
                }
                break;

            case VariantSchema variant:
                foreach (var derivedSchema in variant.DerivedSchemas)
                    Visit(root, derivedSchema, references);
                break;
        }
    }

    private static void VisitFields(TopLevelSchema root, ImmutableArray<Field> fields, HashSet<SchemaName> references)
    {
        foreach (var field in fields)
        {
            Visit(root, field.Type, references);
            Visit(root, field.UnderlyingType, references);
        }
    }

    private static bool EmitsSource(TopLevelSchema schema) =>
        schema is not FixedSchema fixedSchema || fixedSchema.CSharpName != AvroSchema.Bytes.CSharpName;

    private static string GetOwnerPath(IReadOnlyDictionary<SchemaName, string> ownerPaths, SchemaName schemaName) =>
        ownerPaths.TryGetValue(schemaName, out var path)
            ? path
            : throw new InvalidOperationException($"Registered schema '{schemaName}' has no source file owner.");

    private readonly record struct FileRegistration(
        string Path,
        ImmutableArray<SchemaRegistry.SchemaRegistration> Registrations);

    private readonly record struct StrictMissingReference(string SourcePath, SchemaName Reference);
}
