using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AvroSourceGenerator.Avdl;
using AvroSourceGenerator.Avsc;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Exceptions;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Compiler;

public sealed class AvroFile : IEquatable<AvroFile>
{
    internal AvroFile(
        SourceText sourceText,
        AvroSchema? rootSchema,
        ImmutableArray<TopLevelSchema> declarations,
        ImmutableArray<SchemaName> references,
        FrozenDictionary<SchemaName, ImmutableArray<SchemaName>> dependencies,
        ImmutableArray<AvroImport> imports,
        ImmutableArray<AvroDiagnostic> diagnostics,
        AvroParseOptions parseOptions)
    {
        SourceText = sourceText;
        RootSchema = rootSchema;
        Declarations = declarations;
        References = references;
        Dependencies = dependencies;
        Imports = imports;
        Diagnostics = diagnostics;
        ParseOptions = parseOptions;
    }

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

        try
        {
            return sourceText.Type switch
            {
                SourceType.Avsc => AvscSchemaParser.Parse(sourceText, parseOptions),
                SourceType.Avpr => AvscSchemaParser.Parse(sourceText, parseOptions),
                SourceType.Avdl => AvdlSchemaParser.Parse(sourceText, parseOptions),
                _ => throw new InvalidOperationException("Unreachable: Unsupported Avro file type."),
            };
        }
        catch (JsonException ex)
        {
            return Invalid(sourceText, AvroDiagnostic.InvalidJson(SourceSpan.FromException(sourceText, ex), ex.Message), parseOptions);
        }
        catch (InvalidSourceException ex)
        {
            return Invalid(sourceText, ex.Diagnostics, parseOptions);
        }
        catch (InvalidSchemaException ex)
        {
            return Invalid(sourceText, AvroDiagnostic.InvalidSchema(sourceText.GetSpan(0, sourceText.Length), ex.Message), parseOptions);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Invalid(sourceText, AvroDiagnostic.UnknownError(SourceSpan.FromSourceText(sourceText), ex.Message), parseOptions);
        }
    }

    private static AvroFile Invalid(SourceText source, AvroDiagnostic diagnostic, AvroParseOptions parseOptions) =>
        Invalid(source, [diagnostic], parseOptions);

    private static AvroFile Invalid(SourceText sourceText, ImmutableArray<AvroDiagnostic> diagnostics, AvroParseOptions parseOptions) => new(sourceText, null, [], [], [], [], diagnostics, parseOptions);
}
