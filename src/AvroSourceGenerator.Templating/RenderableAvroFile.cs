using System.Collections.Immutable;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Templating;

public sealed class RenderableAvroFile(
    ImmutableArray<TopLevelSchema> emittedSchemas,
    IReadOnlyDictionary<SchemaName, TopLevelSchema> projectSchemas,
    ImmutableArray<BoundAvroFile> contributingFiles,
    RenderOptions options)
    : IEquatable<RenderableAvroFile>
{
    private readonly ImmutableArray<BoundAvroFile> _contributingFiles = contributingFiles;

    public ImmutableArray<TopLevelSchema> EmittedSchemas { get; } = emittedSchemas;

    public IReadOnlyDictionary<SchemaName, TopLevelSchema> ProjectSchemas { get; } = projectSchemas;

    public RenderOptions Options { get; } = options;

    public bool Equals(RenderableAvroFile? other) =>
        ReferenceEquals(this, other) ||
        other is not null &&
        Options == other.Options &&
        HasSameSchemaNames(other) &&
        _contributingFiles.SequenceEqual(other._contributingFiles);

    public override bool Equals(object? obj) => obj is RenderableAvroFile other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Options);
        foreach (var schema in EmittedSchemas)
            hash.Add(schema.SchemaName);
        foreach (var file in _contributingFiles)
            hash.Add(file);
        return hash.ToHashCode();
    }

    private bool HasSameSchemaNames(RenderableAvroFile other)
    {
        if (EmittedSchemas.Length != other.EmittedSchemas.Length)
            return false;

        for (var index = 0; index < EmittedSchemas.Length; index++)
        {
            if (EmittedSchemas[index].SchemaName != other.EmittedSchemas[index].SchemaName)
                return false;
        }

        return true;
    }

    public static RenderableAvroFile Invalid() => new RenderableAvroFile([], new Dictionary<SchemaName, TopLevelSchema>(), [], default);

    public static RenderableAvroFile Create(BoundAvroFile file, AvroCompilation compilation, RenderOptions options, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!compilation.IsValid) return Invalid();
        var emittedSchemas = compilation.GetOwnedDeclarations(file, cancellationToken)
            .Where(static schema => schema.Type is not SchemaType.Fixed || schema.CSharpName != AvroSchema.Bytes.CSharpName)
            .ToImmutableArray();
        var contributingFiles = compilation.GetContributingFiles(emittedSchemas.Select(static schema => schema.SchemaName), cancellationToken);
        return new RenderableAvroFile(emittedSchemas, compilation.Schemas, contributingFiles, options);
    }
}
