using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace AvroSourceGenerator.UnitTests.Infrastructure;

public readonly record struct GeneratorInput(
    Compilation Compilation,
    AnalyzerConfigOptionsProvider OptionsProvider,
    GeneratorDriver GeneratorDriver,
    ImmutableArray<AdditionalText> AdditionalTexts)
{
    public static GeneratorInput Create(ImmutableArray<ProjectFile> projectFiles, ImmutableArray<MetadataReference> references, ProjectConfig projectConfig)
    {
        var parseOptions = new CSharpParseOptions(projectConfig.LanguageVersion);
        var syntaxTrees = projectFiles.Where(f => f.IsSource)
            .Select(source => CSharpSyntaxTree.ParseText(source.Content, parseOptions, source.Hash))
            .ToImmutableArray();
        // A compilation without syntax trees uses the compiler default rather than the requested language version.
        if (syntaxTrees.IsEmpty)
            syntaxTrees = [CSharpSyntaxTree.ParseText(string.Empty, parseOptions)];

        var compilation = CSharpCompilation.Create(
            "GeneratorAssemblyName",
            syntaxTrees,
            (projectConfig.LanguageVersion == LanguageVersion.Preview || projectConfig.LanguageVersion >= (LanguageVersion)1500
                ? PreviewCompilerReferenceAssemblies
                : CompilerReferenceAssemblies).AddRange(references),
            new CSharpCompilationOptions(
                outputKind: OutputKind.DynamicallyLinkedLibrary,
                warningLevel: int.MaxValue));
        var optionsProvider = new AnalyzerConfigOptionsProviderImplementation(projectConfig.GlobalOptions);
        var additionalTexts = projectFiles
            .Where(f => !f.IsSource)
            .Select(text => (AdditionalText)new AdditionalTextImplementation(text))
            .ToImmutableArray();
        var generatorDriver = CSharpGeneratorDriver.Create(
            generators: [new AvroSourceGenerator().AsSourceGenerator()],
            additionalTexts: additionalTexts,
            parseOptions: parseOptions,
            optionsProvider: optionsProvider,
            driverOptions: new GeneratorDriverOptions(
                IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true));

        return new GeneratorInput(compilation, optionsProvider, generatorDriver, additionalTexts);
    }

    private static ImmutableArray<MetadataReference> CompilerReferenceAssemblies
    {
        get => field.IsDefaultOrEmpty
            ? field = Microsoft.CodeAnalysis.Testing.ReferenceAssemblies.Net.Net100
                .ResolveAsync("C#", CancellationToken.None).GetAwaiter().GetResult()
                .Add(MetadataReference.CreateFromFile(typeof(AvroSourceGenerator).Assembly.Location))
            : field;
    }

    private static ImmutableArray<MetadataReference> PreviewCompilerReferenceAssemblies
    {
        get
        {
            if (field.IsDefaultOrEmpty)
            {
                field = Net110.References.All.CastArray<MetadataReference>()
                    .AddRange(MetadataReference.CreateFromFile(typeof(AvroSourceGenerator).Assembly.Location));
            }

            return field;
        }
    }

    private sealed class AdditionalTextImplementation(ProjectFile projectFile) : AdditionalText
    {
        public override string Path => projectFile.Path;

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
