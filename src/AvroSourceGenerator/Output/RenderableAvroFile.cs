using System.Collections.Immutable;
using AvroSourceGenerator.Inputs;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Templating;

namespace AvroSourceGenerator.Output;

/// <summary>
/// The final per-file incremental rendering value. Its equality includes only the file's emitted schemas and the
/// bound files that own their transitive schema dependencies.
/// </summary>
internal sealed class RenderableAvroFile : IEquatable<RenderableAvroFile>
{
    private readonly ImmutableArray<BoundAvroFile> _contributingFiles;

    public RenderableAvroFile(
        ImmutableArray<TopLevelSchema> emittedSchemas,
        ImmutableDictionary<SchemaName, TopLevelSchema> projectSchemas,
        ImmutableArray<BoundAvroFile> contributingFiles,
        RenderOptions options)
    {
        EmittedSchemas = emittedSchemas;
        ProjectSchemas = projectSchemas;
        _contributingFiles = contributingFiles;
        Options = options;
    }

    public ImmutableArray<TopLevelSchema> EmittedSchemas { get; }

    public ImmutableDictionary<SchemaName, TopLevelSchema> ProjectSchemas { get; }

    public RenderOptions Options { get; }

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
    public static RenderableAvroFile Invalid() => new([], [], [], default);

    public static RenderableAvroFile FromInput((BoundAvroFile File, AvroProject ProjecT) input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (file, project) = input;
        return project.CreateRenderableFile(file);
    }
}
