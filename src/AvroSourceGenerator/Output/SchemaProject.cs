using System.Collections.Immutable;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Output;

internal readonly struct SchemaProject : IEquatable<SchemaProject>
{
    public static readonly SchemaProject Empty = new([], [], [], [], []);

    public SchemaProject(
        ImmutableArray<SchemaProjectFile> files,
        ImmutableArray<OwnedSchema> schemas,
        ImmutableArray<SchemaDependency> dependencies,
        ImmutableArray<MissingSchemaReference> missingReferences,
        ImmutableArray<DuplicateSchemaDefinition> duplicates)
    {
        Files = files;
        Schemas = schemas;
        Dependencies = dependencies;
        MissingReferences = missingReferences;
        Duplicates = duplicates;
        ForwardDependencies = CreateIndex(dependencies, static dependency => dependency.Schema, static dependency => dependency.DependsOn);
        ReverseDependencies = CreateIndex(dependencies, static dependency => dependency.DependsOn, static dependency => dependency.Schema);
    }

    public ImmutableArray<SchemaProjectFile> Files { get; }
    public ImmutableArray<OwnedSchema> Schemas { get; }
    public ImmutableArray<SchemaDependency> Dependencies { get; }
    public ImmutableArray<MissingSchemaReference> MissingReferences { get; }
    public ImmutableArray<DuplicateSchemaDefinition> Duplicates { get; }
    public ImmutableDictionary<SchemaName, ImmutableArray<SchemaName>> ForwardDependencies { get; }
    public ImmutableDictionary<SchemaName, ImmutableArray<SchemaName>> ReverseDependencies { get; }

    public bool Equals(SchemaProject other) =>
        Files.SequenceEqual(other.Files) &&
        Schemas.SequenceEqual(other.Schemas) &&
        Dependencies.SequenceEqual(other.Dependencies) &&
        MissingReferences.SequenceEqual(other.MissingReferences) &&
        Duplicates.SequenceEqual(other.Duplicates);

    public override bool Equals(object? obj) => obj is SchemaProject other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        AddRange(ref hash, Files);
        AddRange(ref hash, Schemas);
        AddRange(ref hash, Dependencies);
        AddRange(ref hash, MissingReferences);
        AddRange(ref hash, Duplicates);
        return hash.ToHashCode();
    }

    public static bool operator ==(SchemaProject left, SchemaProject right) => left.Equals(right);
    public static bool operator !=(SchemaProject left, SchemaProject right) => !left.Equals(right);

    private static ImmutableDictionary<SchemaName, ImmutableArray<SchemaName>> CreateIndex(
        ImmutableArray<SchemaDependency> dependencies,
        Func<SchemaDependency, SchemaName> keySelector,
        Func<SchemaDependency, SchemaName> valueSelector) =>
        dependencies
            .GroupBy(keySelector)
            .ToImmutableDictionary(
                static group => group.Key,
                group => group.Select(valueSelector).ToImmutableArray());

    private static void AddRange<T>(ref HashCode hash, ImmutableArray<T> values)
    {
        foreach (var value in values)
            hash.Add(value);
    }
}

internal readonly struct SchemaProjectFile : IEquatable<SchemaProjectFile>
{
    public SchemaProjectFile(string path, ImmutableArray<SchemaName> exports)
    {
        Path = path;
        Exports = exports;
    }

    public string Path { get; }
    public ImmutableArray<SchemaName> Exports { get; }

    public bool Equals(SchemaProjectFile other) =>
        Path == other.Path && Exports.SequenceEqual(other.Exports);

    public override bool Equals(object? obj) => obj is SchemaProjectFile other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Path);
        foreach (var export in Exports)
            hash.Add(export);
        return hash.ToHashCode();
    }

    public static bool operator ==(SchemaProjectFile left, SchemaProjectFile right) => left.Equals(right);
    public static bool operator !=(SchemaProjectFile left, SchemaProjectFile right) => !left.Equals(right);
}

internal readonly record struct OwnedSchema(SchemaName SchemaName, string SourcePath, bool EmitsSource);

internal readonly record struct SchemaDependency(SchemaName Schema, SchemaName DependsOn);

internal readonly record struct MissingSchemaReference(string SourcePath, SchemaName? Schema, SchemaName Reference);

internal readonly record struct DuplicateSchemaDefinition(
    SchemaName SchemaName,
    string OwnerPath,
    string DuplicatePath,
    bool IsIgnored);
