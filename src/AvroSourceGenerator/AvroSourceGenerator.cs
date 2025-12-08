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

        var renderSettings = generatorSettingsProvider.Combine(compilationInfoProvider)
            .Select(Parser.GetRenderSettings)
            .WithTrackingName(TrackingNames.RenderSettings);

        var emitterInputProvider = avroFileProvider.Combine(renderSettings)
            .WithTrackingName(TrackingNames.EmitterInput);

        context.RegisterImplementationSourceOutput(emitterInputProvider, Emitter.Emit);
    }
}
