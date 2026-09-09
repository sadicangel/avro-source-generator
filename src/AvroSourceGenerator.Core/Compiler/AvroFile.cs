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
    private AvroFile(
        SourceText sourceText,
        AvroSchema? rootSchema,
        ImmutableArray<TopLevelSchema> declarations,
        ImmutableArray<SchemaName> references,
        IReadOnlyDictionary<SchemaName, ImmutableArray<SchemaName>> dependencies,
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

    public IReadOnlyDictionary<SchemaName, ImmutableArray<SchemaName>> Dependencies { get; }

    public ImmutableArray<AvroImport> Imports { get; }

    public ImmutableArray<AvroDiagnostic> Diagnostics { get; }

    [MemberNotNullWhen(true, nameof(RootSchema))]
    public bool IsValid => RootSchema is not null && !Diagnostics.HasErrors;

    public AvroParseOptions ParseOptions { get; }

    public bool Equals(AvroFile? other) =>
        ReferenceEquals(this, other) ||
        other is not null &&
        SourceText == other.SourceText &&
        ParseOptions == other.ParseOptions;

    public override bool Equals(object? obj) => obj is AvroFile other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(SourceText, ParseOptions);

    public static AvroFile Parse(SourceText sourceText, AvroParseOptions parseOptions, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

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
            var (rootSchema, declarations, references, dependencies, imports) = sourceText.Type switch
            {
                // TODO: We can now make these return AvroFile directly since we added it now belongs to this project.
                SourceType.Avsc => AvscSchemaParser.Parse(sourceText, parseOptions),
                SourceType.Avpr => AvscSchemaParser.Parse(sourceText, parseOptions),
                SourceType.Avdl => AvdlSchemaParser.Parse(sourceText, parseOptions),
                _ => throw new InvalidOperationException("Unreachable: Unsupported Avro file type."),
            };

            return new AvroFile(
                sourceText,
                rootSchema,
                declarations,
                references,
                dependencies,
                imports,
                [],
                parseOptions);
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

    private static AvroFile Invalid(SourceText sourceText, ImmutableArray<AvroDiagnostic> diagnostics, AvroParseOptions parseOptions) => new AvroFile(sourceText, null, [], [], ImmutableDictionary<SchemaName, ImmutableArray<SchemaName>>.Empty, [], diagnostics, parseOptions);
}
