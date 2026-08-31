using System.Collections.Immutable;
using System.Text;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Inputs;
using AvroSourceGenerator.Output;
using AvroSourceGenerator.Templating;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AvroSourceGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class AvroSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var avroFileProvider = context.AdditionalTextsProvider
            .Where(AvroFile.IsAvroFile)
            .Select(AvroFile.FromAdditionalText)
            .WithTrackingName(TrackingNames.AvroFile);

        var avroFilesProvider = avroFileProvider
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

        var fileRenderInputProvider = avroFileProvider.Combine(generatorOutputProvider)
            .Select(static (input, _) => input.Right.CreateFileRenderInput(input.Left))
            .WithTrackingName(TrackingNames.FileRenderInput);

        var renderedFileProvider = fileRenderInputProvider
            .Select(static (input, _) => AvroTemplate.Render(input.Schemas, input.RegisteredSchemas, input.Settings))
            .WithTrackingName(TrackingNames.RenderedFile);

        context.RegisterImplementationSourceOutput(generatorOutputProvider, EmitDiagnostics);
        context.RegisterImplementationSourceOutput(renderedFileProvider, EmitSchemas);
    }

    private static void EmitDiagnostics(SourceProductionContext context, GeneratorOutput output)
    {
        foreach (var diagnostic in output.Diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void EmitSchemas(SourceProductionContext context, ImmutableArray<RenderedSchema> schemas)
    {
        foreach (var schema in schemas)
        {
            context.AddSource(schema.HintName, SourceText.From(schema.SourceText, Encoding.UTF8));
        }
    }
}
