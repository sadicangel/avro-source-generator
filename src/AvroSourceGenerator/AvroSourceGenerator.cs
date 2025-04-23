using AvroSourceGenerator.Diagnostics;
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

        var avroOptionsProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName("AvroSourceGenerator.AvroAttribute",
                predicate: Parser.IsCandidateDeclaration,
                transform: Parser.GetAvroOptions)
            .WithTrackingName(TrackingNames.AvroOptions);

        var emitterInputProvider = avroFileProvider.Combine(generatorSettingsProvider.Combine(compilationInfoProvider).Combine(avroOptionsProvider.Collect()))
            .Select((source, _) =>
            {
                var (avroFile, ((generatorSettings, compilationInfo), avroOptionsCollection)) = source;

                // There should only be zero or one AvroOptions for each AvroFile.
                // Multiple AvroOptions for the same AvroFile is most likely a compiler error as
                // that means there are two attributes applied to the same declaration or to two
                // different declarations with the same fully qualified name.

                var avroOptions = avroOptionsCollection
                    .FirstOrDefault(options => options.Name == avroFile.SchemaName.Name
                        && options.Namespace == avroFile.SchemaName.Namespace);

                return (avroFile, generatorSettings, compilationInfo, avroOptions);
            })
            .WithTrackingName(TrackingNames.EmitterInput);

        context.RegisterImplementationSourceOutput(emitterInputProvider, Emitter.Emit);

        var diagnosticsProvider = avroOptionsProvider.Combine(avroFileProvider.Collect())
            .Where(source =>
            {
                var (avroOptions, avroFiles) = source;
                return !avroFiles.Any(file => file.SchemaName.Name == avroOptions.Name
                    && file.SchemaName.Namespace == avroOptions.Namespace);
            })
            .Select((source, _) =>
            {
                var (avroOptions, avroFiles) = source;

                return AttributeMismatchDiagnostic.Create(avroOptions.Location, avroOptions.Name, avroOptions.Namespace);
            });

        context.RegisterImplementationSourceOutput(diagnosticsProvider, (context, diagnostic) => context.ReportDiagnostic(diagnostic));
    }
}
