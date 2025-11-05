using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace AvroSourceGenerator.Tests.Setup;

public readonly record struct GeneratorInput(
    Compilation Compilation,
    AnalyzerConfigOptionsProvider OptionsProvider,
    GeneratorDriver GeneratorDriver)
{
    public static GeneratorInput Create(
        ImmutableArray<string> sourceTexts,
        ImmutableArray<string> additionalTexts,
        ImmutableArray<PortableExecutableReference> executableReferences,
        ProjectConfig projectConfig)
    {
        var parseOptions = new CSharpParseOptions(projectConfig.LanguageVersion);
        var optionsProvider = new AnalyzerConfigOptionsProviderImplementation(projectConfig.GlobalOptions);
        var generatorDriver = CreateGeneratorDriver(additionalTexts, parseOptions, optionsProvider);
        var compilation = CreateCompilation(sourceTexts, parseOptions, executableReferences);
        return new GeneratorInput(compilation, optionsProvider, generatorDriver);
    }

    private static CSharpCompilation CreateCompilation(
        ImmutableArray<string> sourceTexts,
        CSharpParseOptions parseOptions,
        ImmutableArray<PortableExecutableReference> executableReferences)
    {
        var syntaxTrees = sourceTexts.Select(source => CSharpSyntaxTree.ParseText(source, parseOptions));
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Concat(
            [
                MetadataReference.CreateFromFile(typeof(AvroSourceGenerator).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(GeneratedCodeAttribute).Assembly.Location),
                .. executableReferences,
            ]);

        var compilation = CSharpCompilation.Create(
            "GeneratorAssemblyName",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(
                outputKind: OutputKind.DynamicallyLinkedLibrary,
                warningLevel: int.MaxValue));

        return compilation;
    }

    private static CSharpGeneratorDriver CreateGeneratorDriver(
        ImmutableArray<string> additionalTexts,
        CSharpParseOptions parseOptions,
        AnalyzerConfigOptionsProvider optionsProvider)
    {
        var generatorDriver = CSharpGeneratorDriver.Create(
            generators: [new AvroSourceGenerator().AsSourceGenerator()],
            additionalTexts: [.. additionalTexts.Select(text => new AdditionalTextImplementation(text))],
            parseOptions: parseOptions,
            optionsProvider: optionsProvider,
            driverOptions: new GeneratorDriverOptions(
                IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true));

        return generatorDriver;
    }

    private sealed class AdditionalTextImplementation(string content) : AdditionalText
    {
        public override string Path => $"schema.avsc";

        public override SourceText GetText(CancellationToken cancellationToken = default) =>
            SourceText.From(content, Encoding.UTF8);
    }

    private sealed class AnalyzerConfigOptionsProviderImplementation(
        IEnumerable<KeyValuePair<string, string>> globalOptions)
        : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } =
            new AnalyzerConfigOptionsImplementation(globalOptions);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;

        private sealed class AnalyzerConfigOptionsImplementation(IEnumerable<KeyValuePair<string, string>> options)
            : AnalyzerConfigOptions
        {
            private readonly Dictionary<string, string> _options = new(
            [
                .. options.Select(kvp => new KeyValuePair<string, string>($"build_property.{kvp.Key}", kvp.Value))
            ]);

            public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
                => _options.TryGetValue(key, out value);
        }
    }
}
