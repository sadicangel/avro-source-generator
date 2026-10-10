using System.Collections.Frozen;
using System.Collections.Immutable;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Templating;

public sealed class RenderableAvroFile(
    BoundAvroFile mainFile,
    ImmutableArray<TopLevelSchema> emittedSchemas,
    FrozenDictionary<SchemaName, TopLevelSchema> projectSchemas,
    ImmutableArray<BoundAvroFile> contributingFiles,
    RenderOptions options)
    : IEquatable<RenderableAvroFile>
{
    private readonly BoundAvroFile _mainFile = mainFile;

    private readonly ImmutableArray<BoundAvroFile> _contributingFiles = contributingFiles;


    public ImmutableArray<TopLevelSchema> EmittedSchemas { get; } = emittedSchemas;

    public FrozenDictionary<SchemaName, TopLevelSchema> ProjectSchemas { get; } = projectSchemas;

    public RenderOptions Options { get; } = options;

    public bool Equals(RenderableAvroFile? other) =>
        ReferenceEquals(this, other) ||
        other is not null &&
        Equals(_mainFile, other._mainFile) &&
        Options == other.Options &&
        HasSameSchemaNames(other) &&
        _contributingFiles.SequenceEqual(other._contributingFiles);

    public override bool Equals(object? obj) => obj is RenderableAvroFile other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_mainFile);
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

    public static RenderableAvroFile Create(BoundAvroFile file, AvroCompilation compilation, RenderOptions options, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contributingFiles = compilation.GetContributingFiles(file, cancellationToken);
        foreach (var contributor in contributingFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!compilation.IsFileValid(contributor, cancellationToken))
                return Invalid(file);
        }
        var emittedSchemas = compilation.GetOwnedDeclarations(file, cancellationToken)
            .Where(static schema => schema.SchemaType is not SchemaType.Fixed || !((FixedSchema)schema).IsSubstituted)
            .ToImmutableArray();
        return new RenderableAvroFile(file, emittedSchemas, compilation.Schemas, contributingFiles, options);
    }

    private static RenderableAvroFile Invalid(BoundAvroFile file) =>
        new RenderableAvroFile(file, [], [], [], default);
}
