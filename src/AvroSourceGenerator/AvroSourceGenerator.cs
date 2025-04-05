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
            .Select(Parser.GetAvroFile);

        var generatorSettings = context.AnalyzerConfigOptionsProvider
            .Select(Parser.GetGeneratorSettings);

        var compilationInfoProvider = context.CompilationProvider
            .Select(Parser.GetCompilationInfo);

        var avroOptionsProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName("AvroSourceGenerator.AvroAttribute",
                predicate: Parser.IsCandidateDeclaration,
                transform: Parser.GetAvroOptions);

        var avroProvider = avroFileProvider.Combine(generatorSettings.Combine(compilationInfoProvider).Combine(avroOptionsProvider.Collect()))
            .Select((source, _) =>
            {
                var (avroFile, ((generatorSettings, compilationInfo), avroOptionsCollection)) = source;

                // There should only be zero or one AvroOptions for each AvroFile.
                // Multiple AvroOptions for the same AvroFile is most likely a compiler error as
                // that means there are two attributes applied to the same declaration or to two
                // different declarations with the same fully qualified name.

                var avroOptions = avroOptionsCollection
                    .FirstOrDefault(options => options.Name == avroFile.Name);

                return (avroFile, generatorSettings, compilationInfo, avroOptions);
            });

        context.RegisterImplementationSourceOutput(avroProvider, Emitter.Emit);

        var diagnosticsProvider = avroOptionsProvider.Combine(avroFileProvider.Collect())
            .Where(source =>
            {
                var (avroOptions, avroFiles) = source;
                return !avroFiles.Any(file => file.Name == avroOptions.Name);
            })
            .Select((source, _) =>
            {
                var (avroOptions, avroFiles) = source;

                return AttributeMismatchDiagnostic.Create(avroOptions.Location, avroOptions.Name, avroOptions.Namespace);
            });

        context.RegisterImplementationSourceOutput(diagnosticsProvider, (context, diagnostic) => context.ReportDiagnostic(diagnostic));
    }
}
