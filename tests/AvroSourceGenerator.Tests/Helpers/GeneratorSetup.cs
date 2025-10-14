using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using AvroSourceGenerator.Configuration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace AvroSourceGenerator.Tests.Helpers;

internal readonly record struct GeneratorSetup(CSharpParseOptions ParseOptions, AnalyzerConfigOptionsProvider OptionsProvider, Compilation Compilation, GeneratorDriver GeneratorDriver)
{
    public static GeneratorSetup Create(
        ImmutableArray<string> sourceTexts,
        ImmutableArray<string> additionalTexts,
        ProjectConfig? projectConfig = null)
    {
        projectConfig ??= ProjectConfig.Default;
        var parseOptions = CreateParseOptions(projectConfig);
        var optionsProvider = CreateOptionsProvider(projectConfig);
        var generatorDriver = CreateGeneratorDriver(additionalTexts, parseOptions, optionsProvider);
        var compilation = CreateCompilation(sourceTexts, parseOptions);
        return new(parseOptions, optionsProvider, compilation, generatorDriver);
    }

    public static CSharpParseOptions CreateParseOptions(ProjectConfig projectConfig) =>
      new(projectConfig.LanguageVersion);

    public static AnalyzerConfigOptionsProvider CreateOptionsProvider(ProjectConfig projectConfig) =>
        new AnalyzerConfigOptionsProviderImplementation(projectConfig.GlobalOptions);

    public static Compilation CreateCompilation(
        ImmutableArray<string> sourceTexts,
        CSharpParseOptions parseOptions)
    {
        var syntaxTrees = sourceTexts.Select(source => CSharpSyntaxTree.ParseText(source, parseOptions));
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Concat([
                MetadataReference.CreateFromFile(typeof(AvroSourceGenerator).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(GeneratedCodeAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Avro.Schema).Assembly.Location),
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

    public static CSharpGeneratorDriver CreateGeneratorDriver(
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

        public override SourceText? GetText(CancellationToken cancellationToken = default) =>
            SourceText.From(content, Encoding.UTF8);
    }

    private sealed class AnalyzerConfigOptionsProviderImplementation(IEnumerable<KeyValuePair<string, string>> globalOptions)
        : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new AnalyzerConfigOptionsImplementation(globalOptions);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;

        private sealed class AnalyzerConfigOptionsImplementation(IEnumerable<KeyValuePair<string, string>> options)
            : AnalyzerConfigOptions
        {
            private readonly Dictionary<string, string> _options = new([
                .. options.Select(kvp => new KeyValuePair<string, string>($"build_property.{kvp.Key}", kvp.Value))
            ]);

            public string this[string key] { get => _options[key]; init => _options[key] = value; }

            public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
                => _options.TryGetValue(key, out value);
        }
    }
}
