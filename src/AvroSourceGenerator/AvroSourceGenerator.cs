using AvroSourceGenerator.Emit;
using AvroSourceGenerator.Parsing;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class AvroSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var avroFileProvider = context.AdditionalTextsProvider
            .Where(Parser.IsAvroFile)
            .Select(Parser.GetAvroFile)
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

        var renderResultProvider = avroFileProvider.Combine(renderSettingsProvider)
            .Select(Renderer.Render)
            .WithTrackingName(TrackingNames.RenderResult);

        context.RegisterImplementationSourceOutput(renderResultProvider, Emitter.Emit);
    }
}
