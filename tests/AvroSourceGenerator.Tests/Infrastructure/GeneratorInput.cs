using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

namespace AvroSourceGenerator.Tests.Infrastructure;

public readonly record struct GeneratorInput(Compilation Compilation, AnalyzerConfigOptionsProvider OptionsProvider, GeneratorDriver GeneratorDriver)
{
    public static GeneratorInput Create(ImmutableArray<ProjectFile> projectFiles, ImmutableArray<MetadataReference> references, ProjectConfig projectConfig)
    {
        var parseOptions = new CSharpParseOptions(projectConfig.LanguageVersion);
        var compilation = CSharpCompilation.Create(
            "GeneratorAssemblyName",
            projectFiles.Where(f => f.IsSource).Select(source => CSharpSyntaxTree.ParseText(source.Content, parseOptions, source.Hash)),
            CompilerReferenceAssemblies.AddRange(references),
            new CSharpCompilationOptions(
                outputKind: OutputKind.DynamicallyLinkedLibrary,
                warningLevel: int.MaxValue));
        var optionsProvider = new AnalyzerConfigOptionsProviderImplementation(projectConfig.GlobalOptions);
        var generatorDriver = CSharpGeneratorDriver.Create(
            generators: [new AvroSourceGenerator().AsSourceGenerator()],
            additionalTexts: [.. projectFiles.Where(f => !f.IsSource).Select(text => new AdditionalTextImplementation(text))],
            parseOptions: parseOptions,
            optionsProvider: optionsProvider,
            driverOptions: new GeneratorDriverOptions(
                IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true));

        return new GeneratorInput(compilation, optionsProvider, generatorDriver);
    }

    private static ImmutableArray<MetadataReference> CompilerReferenceAssemblies
    {
        get
        {
            if (field.IsDefaultOrEmpty)
            {
                field = ReferenceAssemblies.Net.Net100
                    .ResolveAsync("C#", CancellationToken.None).GetAwaiter().GetResult()
                    .AddRange(MetadataReference.CreateFromFile(typeof(AvroSourceGenerator).Assembly.Location));
            }

            return field;
        }
    }

    private sealed class AdditionalTextImplementation(ProjectFile projectFile) : AdditionalText
    {
        public override string Path => projectFile.Hash;

        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(projectFile.Content, Encoding.UTF8);
    }

    private sealed class AnalyzerConfigOptionsProviderImplementation(IEnumerable<KeyValuePair<string, string>> globalOptions) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new AnalyzerConfigOptionsImplementation(globalOptions);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;
    }

    private sealed class AnalyzerConfigOptionsImplementation(IEnumerable<KeyValuePair<string, string>> options)
        : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> _options = new Dictionary<string, string>([.. options.Select(kvp => new KeyValuePair<string, string>($"build_property.{kvp.Key}", kvp.Value))]);
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) => _options.TryGetValue(key, out value);
    }
}
