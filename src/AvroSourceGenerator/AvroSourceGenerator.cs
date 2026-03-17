using System.Text;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Inputs;
using AvroSourceGenerator.Output;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AvroSourceGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class AvroSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var avroFilesProvider = context.AdditionalTextsProvider
            .Where(AvroFile.IsAvroFile)
            .Select(AvroFile.FromAdditionalText)
            .Collect()
            .WithTrackingName(TrackingNames.AvroFiles);

        var projectSettingsProvider = context.AnalyzerConfigOptionsProvider
            .Select(ProjectSettings.FromOptions)
            .WithTrackingName(TrackingNames.ProjectSettings);

        var compilationInfoProvider = context.CompilationProvider
            .Select(CompilationInfo.FromCompilation)
            .WithTrackingName(TrackingNames.CompilationInfo);

        var generatorConfigProvider = projectSettingsProvider.Combine(compilationInfoProvider)
            .Select(GeneratorConfig.FromEnvironment)
            .WithTrackingName(TrackingNames.RenderSettings);

        var generatorOutputProvider = avroFilesProvider.Combine(generatorConfigProvider)
            .Select(GeneratorOutput.FromInput)
            .WithTrackingName(TrackingNames.GeneratorOutput);

        context.RegisterImplementationSourceOutput(generatorOutputProvider, Emit);
    }

    private static void Emit(SourceProductionContext context, GeneratorOutput output)
    {
        var (schemas, diagnostics) = output;

        foreach (var diagnostic in diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }

        foreach (var schema in schemas)
        {
            context.AddSource(schema.HintName, SourceText.From(schema.SourceText, Encoding.UTF8));
        }
    }
}
