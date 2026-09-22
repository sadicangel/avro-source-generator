using System.Collections.Frozen;
using System.Collections.Immutable;
using AvroSourceGenerator.Avdl;
using AvroSourceGenerator.Avsc;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Compiler;

public sealed class AvroFile : IEquatable<AvroFile>, ISourceFile
{
    internal AvroFile(
        SourceText sourceText,
        AvroSchema rootSchema,
        ImmutableArray<TopLevelSchema> declarations,
        ImmutableArray<SourceSpan> declarationSpans,
        ImmutableArray<SchemaName> references,
        FrozenDictionary<SchemaName, ImmutableArray<SourceSpan>> referenceSpans,
        FrozenDictionary<SchemaName, ImmutableArray<SchemaName>> dependencies,
        ImmutableArray<AvroImport> imports,
        ImmutableArray<AvroDiagnostic> diagnostics,
        AvroParseOptions parseOptions)
    {
        Text = sourceText;
        RootSchema = rootSchema;
        Declarations = declarations;
        DeclarationSpans = declarationSpans;
        References = references;
        ReferenceSpans = referenceSpans;
        Dependencies = dependencies;
        Imports = imports;
        Diagnostics = diagnostics;
        ParseOptions = parseOptions;
    }

    internal ImmutableArray<SourceSpan> DeclarationSpans { get; }
    internal FrozenDictionary<SchemaName, ImmutableArray<SourceSpan>> ReferenceSpans { get; }

    public SourceText Text { get; }

    public SourcePath Path => Text.Path;

    public AvroSchema RootSchema { get; }

    public ImmutableArray<TopLevelSchema> Declarations { get; }

    public ImmutableArray<SchemaName> References { get; }

    public FrozenDictionary<SchemaName, ImmutableArray<SchemaName>> Dependencies { get; }

    public ImmutableArray<AvroImport> Imports { get; }

    public ImmutableArray<AvroDiagnostic> Diagnostics { get; }

    public bool IsValid => !Diagnostics.HasErrors;

    public AvroParseOptions ParseOptions { get; }

    public bool Equals(AvroFile? other) =>
        ReferenceEquals(this, other) ||
        other is not null &&
        Text == other.Text &&
        ParseOptions == other.ParseOptions;

    public override bool Equals(object? obj) => obj is AvroFile other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Text, ParseOptions);

    public static AvroFile Parse(SourceText sourceText, AvroParseOptions parseOptions, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!sourceText.Path.TryGetSourceType(out var sourceType))
            return Invalid(sourceText, AvroDiagnostic.UnsupportedSourceType(SourceSpan.FromSourceText(sourceText)), parseOptions);

        if (string.IsNullOrWhiteSpace(sourceText.Text))
        {
            return Invalid(
                sourceText,
                AvroDiagnostic.EmptySource(SourceSpan.FromSourceText(sourceText)),
                parseOptions);
        }

        return sourceType is SourceType.Avdl
            ? AvdlSchemaParser.Parse(sourceText, parseOptions, cancellationToken)
            : AvscSchemaParser.Parse(sourceText, parseOptions, cancellationToken);
    }

    internal static AvroFile Invalid(SourceText source, AvroDiagnostic diagnostic, AvroParseOptions parseOptions) =>
        Invalid(source, [diagnostic], parseOptions);

    internal static AvroFile Invalid(SourceText sourceText, ImmutableArray<AvroDiagnostic> diagnostics, AvroParseOptions parseOptions) => new(sourceText, AvroSchema.Null, [], [], [], [], [], [], diagnostics, parseOptions);
}
