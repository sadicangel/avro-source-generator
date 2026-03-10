using AvroSourceGenerator.Emit;
using AvroSourceGenerator.Parsing;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class AvroSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var avroFilesProvider = context.AdditionalTextsProvider
            .Where(Parser.IsAvroFile)
            .Select(Parser.GetAvroFile)
            .Collect()
            .WithTrackingName(TrackingNames.AvroFiles);

        var generatorSettingsProvider = context.AnalyzerConfigOptionsProvider
            .Select(Parser.GetGeneratorSettings)
            .WithTrackingName(TrackingNames.GeneratorSettings);

        var compilationInfoProvider = context.CompilationProvider
            .Select(Parser.GetCompilationInfo)
            .WithTrackingName(TrackingNames.CompilationInfo);

        var renderSettingsProvider = generatorSettingsProvider.Combine(compilationInfoProvider)
            .Select(Parser.GetRenderSettings)
            .WithTrackingName(TrackingNames.RenderSettings);

        var renderResultProvider = avroFilesProvider
            .Combine(renderSettingsProvider)
            .Select(Renderer.Render)
            .WithTrackingName(TrackingNames.RenderResult);

        // TODO: We probably don't need this tracking name/step anymore.
        var renderResultsProvider = renderResultProvider
            .WithTrackingName(TrackingNames.Emitter);

        context.RegisterImplementationSourceOutput(renderResultsProvider, Emitter.Emit);
    }
}
