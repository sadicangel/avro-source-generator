using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AvroSourceGenerator.Avdl;
using AvroSourceGenerator.Avsc;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Exceptions;
using AvroSourceGenerator.Schemas;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Inputs;

internal sealed class AvroFile : IEquatable<AvroFile>
{
    private AvroFile(
        SourceText sourceText,
        AvroSchema? rootSchema,
        ImmutableArray<TopLevelSchema> declarations,
        ImmutableArray<SchemaName> references,
        IReadOnlyDictionary<SchemaName, ImmutableArray<SchemaName>> dependencies,
        ImmutableArray<string> imports,
        ImmutableArray<DiagnosticInfo> diagnostics,
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

    public ImmutableArray<string> Imports { get; }

    public ImmutableArray<DiagnosticInfo> Diagnostics { get; }

    [MemberNotNullWhen(true, nameof(RootSchema))]
    public bool IsValid => !Diagnostics.Any(d => d.Descriptor.DefaultSeverity is Microsoft.CodeAnalysis.DiagnosticSeverity.Error);

    public AvroParseOptions ParseOptions { get; }

    public bool Equals(AvroFile? other) =>
        ReferenceEquals(this, other) ||
        other is not null &&
        SourceText == other.SourceText &&
        ParseOptions == other.ParseOptions;

    public override bool Equals(object? obj) => obj is AvroFile other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(SourceText, ParseOptions);

    public static AvroFile FromInput((SourceText, AvroParseOptions) input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (sourceText, parseOptions) = input;
        if (string.IsNullOrWhiteSpace(sourceText.Text))
        {
            return Invalid(
                sourceText,
                InvalidJsonDiagnostic.Create(LocationInfo.FromSourceFile(sourceText.Path, sourceText.Text), "The file is empty."),
                parseOptions);
        }

        try
        {
            var (rootSchema, declarations, references, dependencies, imports) = sourceText.Type switch
            {
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
            return Invalid(sourceText, InvalidJsonDiagnostic.Create(LocationInfo.FromException(sourceText.Path, sourceText.Text, ex), ex.Message), parseOptions);
        }
        catch (InvalidSourceException ex)
        {
            return Invalid(
                sourceText,
                [.. ex.Diagnostics.Select(InvalidSyntaxDiagnostic.Create)],
                parseOptions);
        }
        catch (InvalidSchemaException ex)
        {
            return Invalid(sourceText, InvalidSchemaDiagnostic.Create(LocationInfo.FromSourceFile(sourceText.Path, sourceText.Text), ex.Message), parseOptions);
        }
        catch (Exception ex)
        {
            return Invalid(sourceText, UnknownErrorDiagnostic.Create(LocationInfo.FromSourceFile(sourceText.Path, sourceText.Text), ex.Message), parseOptions);
        }
    }

    private static AvroFile Invalid(SourceText source, DiagnosticInfo diagnostic, AvroParseOptions parseOptions) =>
        Invalid(source, [diagnostic], parseOptions);

    private static AvroFile Invalid(SourceText sourceText, ImmutableArray<DiagnosticInfo> diagnostics, AvroParseOptions parseOptions) =>
        new(
            sourceText,
            null,
            [],
            [],
            ImmutableDictionary<SchemaName, ImmutableArray<SchemaName>>.Empty,
            [],
            diagnostics,
            parseOptions);
}
