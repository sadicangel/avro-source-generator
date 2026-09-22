using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Compiler;

public sealed record AvroImport
{
    public AvroImport(AvroImportKind kind, string path, SourceSpan sourceSpan)
    {
        if (sourceSpan.IsNone)
            throw new ArgumentException("An import must have a source SourceSpan.", nameof(sourceSpan));

        Kind = kind;
        Path = path;
        SourceSpan = sourceSpan;
    }

    public AvroImportKind Kind { get; }
    public string Path { get; }
    public SourceSpan SourceSpan { get; }
}
