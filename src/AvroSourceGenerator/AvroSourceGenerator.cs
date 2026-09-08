using System.Collections.Immutable;
using System.Text;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Extensions;
using AvroSourceGenerator.Templating;
using AvroSourceGenerator.Text;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class AvroSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var projectPropertiesProvider = context.AnalyzerConfigOptionsProvider
            .Select(ProjectProperties.FromAnalyzerOptions)
            .WithTrackingName(TrackingNames.ProjectProperties);

        var compilationEnvironmentProvider = context.CompilationProvider
            .Select(CompilationEnvironment.FromCompilation)
            .WithTrackingName(TrackingNames.CompilationEnvironment);

        var generatorConfigurationProvider = projectPropertiesProvider.Combine(compilationEnvironmentProvider)
            .Select(GeneratorConfiguration.Resolve)
            .WithTrackingName(TrackingNames.GeneratorConfiguration);

        var parseOptionsProvider = generatorConfigurationProvider
            .Select(AvroParseOptions.FromGeneratorConfiguration)
            .WithTrackingName(TrackingNames.AvroParseOptions);

        var compilationOptionsProvider = generatorConfigurationProvider
            .Select(AvroCompilationOptions.FromGeneratorConfiguration);

        var renderOptionsProvider = generatorConfigurationProvider
            .Select(RenderOptions.FromGeneratorConfiguration);

        var sourceTextProvider = context.AdditionalTextsProvider
            .Where(SourceText.IsAvroFile)
            .Select(SourceText.FromAdditionalText)
            .WithTrackingName(TrackingNames.SourceText);

        var avroFileProvider = sourceTextProvider.Combine(parseOptionsProvider)
            .Select(AvroFile.Parse)
            .WithTrackingName(TrackingNames.AvroFile);

        var avroFilesProvider = avroFileProvider
            .Collect()
            .WithTrackingName(TrackingNames.AvroFiles);

        var symbolTableProvider = avroFilesProvider
            .Select(SymbolTable.FromFiles)
            .WithTrackingName(TrackingNames.SymbolTable);

        var linkedAvroFileProvider = avroFileProvider.Combine(symbolTableProvider)
            .Select(LinkedAvroFile.Link)
            .WithTrackingName(TrackingNames.LinkedAvroFile);

        var boundAvroFileProvider = linkedAvroFileProvider
            .Select(BoundAvroFile.Bind)
            .WithTrackingName(TrackingNames.BoundAvroFile);

        var boundAvroFilesProvider = boundAvroFileProvider
            .Collect()
            .WithTrackingName(TrackingNames.BoundAvroFiles);

        var avroCompilationProvider = boundAvroFilesProvider
            .Combine(compilationOptionsProvider)
            .Select(AvroCompilation.FromInput)
            .WithTrackingName(TrackingNames.AvroCompilation);

        var renderableAvroFileProvider = boundAvroFileProvider.Combine(avroCompilationProvider).Combine(renderOptionsProvider)
            .Select(RenderableAvroFile.FromInput)
            .WithTrackingName(TrackingNames.RenderableAvroFile);

        var renderedFileProvider = renderableAvroFileProvider
            .Select(AvroTemplate.Render)
            .WithTrackingName(TrackingNames.RenderedFile);

        context.RegisterImplementationSourceOutput(generatorConfigurationProvider, EmitDiagnostics);
        context.RegisterImplementationSourceOutput(avroCompilationProvider, EmitDiagnostics);
        context.RegisterImplementationSourceOutput(renderedFileProvider, EmitSchemas);
    }

    private static void EmitDiagnostics(SourceProductionContext context, GeneratorConfiguration configuration) =>
        EmitDiagnostics(context, configuration.Diagnostics);

    private static void EmitDiagnostics(SourceProductionContext context, AvroCompilation compilation) =>
        EmitDiagnostics(context, compilation.Diagnostics);

    private static void EmitDiagnostics(SourceProductionContext context, ImmutableArray<AvroDiagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            context.ReportDiagnostic(diagnostic.ToDiagnostic());
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
