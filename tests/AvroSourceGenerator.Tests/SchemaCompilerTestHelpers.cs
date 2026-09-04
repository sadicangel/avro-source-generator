using System.Collections.Immutable;
using AvroSourceGenerator.Avdl;
using AvroSourceGenerator.Avsc;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Inputs;
using AvroSourceGenerator.Output;
using AvroSourceGenerator.Templating;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Tests;

internal static class SchemaCompilerTestHelpers
{
    public static ParseResult ParseJson(
        string json,
        TargetProfile targetProfile = TargetProfile.Modern,
        bool useNullableReferenceTypes = true) =>
        AvscSchemaParser.Parse(
            new SourceText("test.avsc", json),
            new AvroParseOptions(targetProfile, useNullableReferenceTypes));

    public static ParseResult ParseSource(
        string source,
        TargetProfile targetProfile = TargetProfile.Modern,
        bool useNullableReferenceTypes = true) =>
        AvdlSchemaParser.Parse(
            new SourceText("test.avdl", source),
            new AvroParseOptions(targetProfile, useNullableReferenceTypes));

    public static AvroProject Bind(
        ReferenceResolution referenceResolution,
        DuplicateResolution duplicateResolution,
        params (string Path, string Text)[] sources) =>
        CompileProject(TargetProfile.Modern, referenceResolution, duplicateResolution, sources).Project;

    public static CompiledAvroProject CompileProject(
        TargetProfile targetProfile,
        ReferenceResolution referenceResolution,
        DuplicateResolution duplicateResolution,
        params (string Path, string Text)[] sources)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var options = new AvroProjectOptions(
            targetProfile,
            LanguageFeatures.Latest,
            AccessModifier.Public,
            referenceResolution,
            duplicateResolution,
            Diagnostics: []);
        var files = sources
            .Select(source => AvroFile.FromInput(
                (new SourceText(source.Path, source.Text), new AvroParseOptions(
                    options.TargetProfile,
                    options.LanguageFeatures.HasFlag(LanguageFeatures.NullableReferenceTypes))),
                cancellationToken))
            .ToImmutableArray();
        var symbolTable = SymbolTable.FromFiles(files, cancellationToken);
        var boundFiles = files
            .Select(file => LinkedAvroFile.FromInput((file, symbolTable), cancellationToken))
            .Select(file => BoundAvroFile.FromInput(file, cancellationToken))
            .ToImmutableArray();
        var project = AvroProject.FromInput((boundFiles, options), cancellationToken);
        var renderableFiles = boundFiles
            .Select(project.CreateRenderableFile)
            .ToImmutableArray();
        return new CompiledAvroProject(files, symbolTable, boundFiles, project, renderableFiles);
    }
}

internal readonly record struct CompiledAvroProject(
    ImmutableArray<AvroFile> Files,
    SymbolTable SymbolTable,
    ImmutableArray<BoundAvroFile> BoundFiles,
    AvroProject Project,
    ImmutableArray<RenderableAvroFile> RenderableFiles);
