using System.Collections.Immutable;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Templating;

namespace AvroSourceGenerator.Output;

/// <summary>
/// The rendering input for one Avro source file.
///
/// Equality deliberately uses the stable semantic fingerprint rather than the schema object graph. This lets
/// the incremental generator retain a previous file input when rebuilding the project registry produces an
/// equivalent set of exports and dependencies for that file.
/// </summary>
internal readonly struct FileRenderInput : IEquatable<FileRenderInput>
{
    private readonly ImmutableArray<string> _fingerprint;

    public FileRenderInput(
        string path,
        ImmutableArray<TopLevelSchema> schemas,
        ImmutableDictionary<SchemaName, TopLevelSchema> registeredSchemas,
        TemplateSettings settings,
        ImmutableArray<string> fingerprint)
    {
        Path = path;
        Schemas = schemas;
        RegisteredSchemas = registeredSchemas;
        Settings = settings;
        _fingerprint = fingerprint;
    }

    public string Path { get; }
    public ImmutableArray<TopLevelSchema> Schemas { get; }
    public ImmutableDictionary<SchemaName, TopLevelSchema> RegisteredSchemas { get; }
    public TemplateSettings Settings { get; }

    public bool Equals(FileRenderInput other) =>
        Path == other.Path &&
        Settings == other.Settings &&
        _fingerprint.SequenceEqual(other._fingerprint);

    public override bool Equals(object? obj) => obj is FileRenderInput other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Path);
        hash.Add(Settings);
        foreach (var value in _fingerprint)
            hash.Add(value);
        return hash.ToHashCode();
    }

    public static bool operator ==(FileRenderInput left, FileRenderInput right) => left.Equals(right);
    public static bool operator !=(FileRenderInput left, FileRenderInput right) => !left.Equals(right);
}
