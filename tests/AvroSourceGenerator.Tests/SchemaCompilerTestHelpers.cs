using System.Collections.Immutable;
using AvroSourceGenerator.Avdl;
using AvroSourceGenerator.Avsc;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Templating;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Tests;

internal static class SchemaCompilerTestHelpers
{
    public static ParseResult ParseJson(
        string json,
        GenerationTarget generationTarget = GenerationTarget.Modern,
        bool useNullableReferenceTypes = true) =>
        AvscSchemaParser.Parse(
            new SourceText("test.avsc", json),
            new AvroParseOptions(generationTarget, useNullableReferenceTypes));

    public static ParseResult ParseSource(
        string source,
        GenerationTarget generationTarget = GenerationTarget.Modern,
        bool useNullableReferenceTypes = true) =>
        AvdlSchemaParser.Parse(
            new SourceText("test.avdl", source),
            new AvroParseOptions(generationTarget, useNullableReferenceTypes));

    public static AvroCompilation Bind(
        ReferenceResolution referenceResolution,
        DuplicateResolution duplicateResolution,
        params (string Path, string Text)[] sources) =>
        Compile(GenerationTarget.Modern, referenceResolution, duplicateResolution, sources).Compilation;

    public static CompiledAvroSources Compile(
        GenerationTarget generationTarget,
        ReferenceResolution referenceResolution,
        DuplicateResolution duplicateResolution,
        params (string Path, string Text)[] sources)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var configuration = new GeneratorConfiguration(
            generationTarget,
            LanguageFeatures.Latest,
            AccessModifier.Public,
            referenceResolution,
            duplicateResolution,
            Diagnostics: []);
        var files = sources
            .Select(source => AvroFile.Parse(
                new SourceText(source.Path, source.Text), new AvroParseOptions(
                    configuration.GenerationTarget,
                    configuration.LanguageFeatures.HasFlag(LanguageFeatures.NullableReferenceTypes)),
                cancellationToken))
            .ToImmutableArray();
        var symbolTable = SymbolTable.FromFiles(files, cancellationToken);
        var boundFiles = files
            .Select(file => LinkedAvroFile.Link(file, symbolTable, cancellationToken))
            .Select(file => BoundAvroFile.Bind(file, cancellationToken))
            .ToImmutableArray();
        var compilation = AvroCompilation.Create(boundFiles, new AvroCompilationOptions(referenceResolution, duplicateResolution), cancellationToken);
        var renderableFiles = boundFiles
            .Select(file => RenderableAvroFile.Create(file, compilation, new RenderOptions(generationTarget, configuration.LanguageFeatures, configuration.AccessModifier), cancellationToken))
            .ToImmutableArray();
        return new CompiledAvroSources(files, symbolTable, boundFiles, compilation, renderableFiles);
    }
}

internal readonly record struct CompiledAvroSources(
    ImmutableArray<AvroFile> Files,
    SymbolTable SymbolTable,
    ImmutableArray<BoundAvroFile> BoundFiles,
    AvroCompilation Compilation,
    ImmutableArray<RenderableAvroFile> RenderableFiles);
