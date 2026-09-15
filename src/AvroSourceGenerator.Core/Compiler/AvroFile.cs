using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using AvroSourceGenerator.Avdl;
using AvroSourceGenerator.Avsc;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Compiler;

public sealed class AvroFile : IEquatable<AvroFile>
{
    internal AvroFile(
        SourceText sourceText,
        AvroSchema? rootSchema,
        ImmutableArray<TopLevelSchema> declarations,
        ImmutableArray<SourceSpan> declarationSpans,
        ImmutableArray<SchemaName> references,
        FrozenDictionary<SchemaName, ImmutableArray<SourceSpan>> referenceSpans,
        FrozenDictionary<SchemaName, ImmutableArray<SchemaName>> dependencies,
        ImmutableArray<AvroImport> imports,
        ImmutableArray<AvroDiagnostic> diagnostics,
        AvroParseOptions parseOptions)
    {
        SourceText = sourceText;
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

    public SourceText SourceText { get; }

    public AvroSchema? RootSchema { get; }

    public ImmutableArray<TopLevelSchema> Declarations { get; }

    public ImmutableArray<SchemaName> References { get; }

    public FrozenDictionary<SchemaName, ImmutableArray<SchemaName>> Dependencies { get; }

    public ImmutableArray<AvroImport> Imports { get; }

    public ImmutableArray<AvroDiagnostic> Diagnostics { get; }

    [MemberNotNullWhen(true, nameof(RootSchema))]
    public bool IsValid => RootSchema is not null && !Diagnostics.HasErrors;

    public AvroParseOptions ParseOptions { get; }

    public bool Equals(AvroFile? other) =>
        ReferenceEquals(this, other) ||
        other is not null &&
        SourceText.Equals(other.SourceText) &&
        ParseOptions == other.ParseOptions;

    public override bool Equals(object? obj) => obj is AvroFile other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(SourceText, ParseOptions);

    public static AvroFile Parse(SourceText sourceText, AvroParseOptions parseOptions, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!sourceText.Path.EndsWith(".avsc", StringComparison.OrdinalIgnoreCase)
            && !sourceText.Path.EndsWith(".avpr", StringComparison.OrdinalIgnoreCase)
            && !sourceText.Path.EndsWith(".avdl", StringComparison.OrdinalIgnoreCase))
            return Invalid(sourceText, AvroDiagnostic.InvalidSource(SourceSpan.FromSourceText(sourceText), "Unsupported Avro file type."), parseOptions);

        if (string.IsNullOrWhiteSpace(sourceText.Text))
        {
            return Invalid(
                sourceText,
                // TODO: This should be a different diagnostic, since this can be any Avro file type, not just JSON.
                AvroDiagnostic.InvalidJson(SourceSpan.FromSourceText(sourceText), "The file is empty."),
                parseOptions);
        }

        return sourceText.Type switch
        {
            SourceType.Avsc or SourceType.Avpr => AvscSchemaParser.Parse(sourceText, parseOptions),
            SourceType.Avdl => AvdlSchemaParser.Parse(sourceText, parseOptions),
            _ => throw new InvalidOperationException("Unreachable: Unsupported Avro file type."),
        };
    }

    internal static AvroFile Invalid(SourceText source, AvroDiagnostic diagnostic, AvroParseOptions parseOptions) =>
        Invalid(source, [diagnostic], parseOptions);

    internal static AvroFile Invalid(SourceText sourceText, ImmutableArray<AvroDiagnostic> diagnostics, AvroParseOptions parseOptions) =>
        new(sourceText, null, [], [], [], [], [], [], diagnostics, parseOptions);
}
