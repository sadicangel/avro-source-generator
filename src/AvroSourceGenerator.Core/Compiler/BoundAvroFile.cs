using System.Collections.Frozen;
using System.Collections.Immutable;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Compiler;

public sealed class BoundAvroFile : IEquatable<BoundAvroFile>, ISourceFile
{
    private readonly LinkedAvroFile _linkedFile;

    private BoundAvroFile(LinkedAvroFile linkedFile, AvroSchema? rootSchema, ImmutableArray<TopLevelSchema> declarations)
    {
        _linkedFile = linkedFile;
        RootSchema = rootSchema;
        Declarations = declarations;
    }

    public AvroFile File => _linkedFile.File;

    public SourceText Text => File.Text;

    public SourcePath Path => File.Path;

    public bool IsValid => File.IsValid;

    public AvroSchema? RootSchema { get; }

    public ImmutableArray<TopLevelSchema> Declarations { get; }

    public FrozenDictionary<SchemaName, CSharpName?> References => _linkedFile.References;

    public FrozenDictionary<SchemaName, ImmutableArray<SchemaName>> Dependencies => File.Dependencies;

    public bool Equals(BoundAvroFile? other) =>
        ReferenceEquals(this, other) ||
        other is not null && _linkedFile.Equals(other._linkedFile);

    public override bool Equals(object? obj) => obj is BoundAvroFile other && Equals(other);

    public override int GetHashCode() => _linkedFile.GetHashCode();

    public static BoundAvroFile Bind(LinkedAvroFile linkedFile, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var file = linkedFile.File;
        if (!file.IsValid)
            return new BoundAvroFile(linkedFile, null, []);

        var binder = new SchemaBinder(linkedFile, cancellationToken);
        var rootSchema = binder.Bind(file.RootSchema);
        var declarations = ImmutableArray.CreateBuilder<TopLevelSchema>(file.Declarations.Length);
        foreach (var declaration in file.Declarations)
            declarations.Add((TopLevelSchema)binder.Bind(declaration));

        return new BoundAvroFile(linkedFile, rootSchema, declarations.MoveToImmutable());
    }
}
