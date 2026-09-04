using System.Collections.Immutable;
using System.Text;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Inputs;
using AvroSourceGenerator.Output;
using AvroSourceGenerator.Templating;
using AvroSourceGenerator.Text;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class AvroSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var csharpProjectOptionsProvider = context.AnalyzerConfigOptionsProvider
            .Select(CSharpProjectOptions.FromOptions)
            .WithTrackingName(TrackingNames.ProjectSettings);

        var compilationInfoProvider = context.CompilationProvider
            .Select(CompilationInfo.FromCompilation)
            .WithTrackingName(TrackingNames.CompilationInfo);

        var avroProjectOptionsProvider = csharpProjectOptionsProvider.Combine(compilationInfoProvider)
            .Select(AvroProjectOptions.FromEnvironment)
            .WithTrackingName(TrackingNames.AvroProjectOptions);

        var parseOptionsProvider = avroProjectOptionsProvider
            .Select(AvroParseOptions.FromAvroProjectOptions)
            .WithTrackingName(TrackingNames.AvroParseOptions);

        var sourceTextProvider = context.AdditionalTextsProvider
            .Where(SourceText.IsAvroFile)
            .Select(SourceText.FromAdditionalText)
            .WithTrackingName(TrackingNames.SourceText);

        var avroFileProvider = sourceTextProvider.Combine(parseOptionsProvider)
            .Select(AvroFile.FromInput)
            .WithTrackingName(TrackingNames.AvroFile);

        var avroFilesProvider = avroFileProvider
            .Collect()
            .WithTrackingName(TrackingNames.AvroFiles);

        var symbolTableProvider = avroFilesProvider
            .Select(SymbolTable.FromFiles)
            .WithTrackingName(TrackingNames.SymbolTable);

        var linkedAvroFileProvider = avroFileProvider.Combine(symbolTableProvider)
            .Select(LinkedAvroFile.FromInput)
            .WithTrackingName(TrackingNames.LinkedAvroFile);

        var boundAvroFileProvider = linkedAvroFileProvider
            .Select(BoundAvroFile.FromInput)
            .WithTrackingName(TrackingNames.BoundAvroFile);

        var boundAvroFilesProvider = boundAvroFileProvider
            .Collect()
            .WithTrackingName(TrackingNames.BoundAvroFiles);

        var avroProjectProvider = boundAvroFilesProvider
            .Combine(avroProjectOptionsProvider)
            .Select(AvroProject.FromInput)
            .WithTrackingName(TrackingNames.AvroProject);

        var renderableAvroFileProvider = boundAvroFileProvider.Combine(avroProjectProvider)
            .Select(RenderableAvroFile.FromInput)
            .WithTrackingName(TrackingNames.RenderableAvroFile);

        var renderedFileProvider = renderableAvroFileProvider
            .Select(AvroTemplate.Render)
            .WithTrackingName(TrackingNames.RenderedFile);

        context.RegisterImplementationSourceOutput(avroProjectProvider, EmitDiagnostics);
        context.RegisterImplementationSourceOutput(renderedFileProvider, EmitSchemas);
    }

    private static void EmitDiagnostics(SourceProductionContext context, AvroProject project)
    {
        foreach (var diagnostic in project.Diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void EmitSchemas(SourceProductionContext context, ImmutableArray<RenderedSchema> schemas)
    {
        foreach (var schema in schemas)
        {
            context.AddSource(
                schema.HintName,
                Microsoft.CodeAnalysis.Text.SourceText.From(schema.SourceText, Encoding.UTF8));
        }
    }
}
