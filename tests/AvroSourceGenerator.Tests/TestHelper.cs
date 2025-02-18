using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace AvroSourceGenerator.Tests;

public readonly record struct Document(string FileName, string Content);
public readonly record struct GeneratedOutput(ImmutableArray<Diagnostic> Diagnostics, ImmutableArray<Document> Documents);

internal static class TestHelper
{
    public static SettingsTask Verify(string avro, string? source = null) =>
        Verifier.Verify(GenerateOutput(source is null ? [] : [source], [avro]));

    public static SettingsTask VerifySourceCode(
        string schema,
        string? source = null,
        ProjectConfig? config = null)
    {
        var (_, documents) = GenerateOutput(
            source is null ? [] : [source],
            [schema],
            config);
        return Verifier.Verify(Assert.Single(documents).Content);
    }

    public static SettingsTask VerifyDiagnostic(
        string schema,
        string? source = null,
        ProjectConfig? config = null)
    {
        var (diagnostics, _) = GenerateOutput(
            source is null ? [] : [source],
            [schema],
            config);
        return Verifier.Verify(Assert.Single(diagnostics));
    }

    public static GeneratedOutput GenerateOutput(
        ImmutableArray<string> sourceTexts,
        ImmutableArray<string> additionalTexts,
        ProjectConfig? projectConfig = null)
    {
        projectConfig ??= ProjectConfig.Default;

        var parseOptions = new CSharpParseOptions(projectConfig.LanguageVersion);
        var syntaxTrees = sourceTexts.Select(source => CSharpSyntaxTree.ParseText(source, parseOptions));
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Concat(
            [
                MetadataReference.CreateFromFile(typeof(AvroSourceGenerator).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(AvroAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(GeneratedCodeAttribute).Assembly.Location),
            ]);

        var compilation = CSharpCompilation.Create(
            "GeneratorAssemblyName",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        CSharpGeneratorDriver
            .Create(new AvroSourceGenerator())
            .AddAdditionalTexts([.. additionalTexts.Select(t => new AdditionalTextImplementation(t))])
            .WithUpdatedParseOptions(parseOptions)
            .WithUpdatedAnalyzerConfigOptions(new AnalyzerConfigOptionsProviderImplementation(projectConfig.GlobalOptions))
            .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var documents = outputCompilation.SyntaxTrees
            .Where(st => !string.IsNullOrEmpty(st.FilePath))
            .Select(st => new Document(st.FilePath.Replace('\\', '/'), st.ToString()))
            .ToImmutableArray();

        return new(diagnostics, documents);
    }
}

file sealed class AdditionalTextImplementation(string content) : AdditionalText
{
    public override string Path => $"schema.avsc";

    public override SourceText? GetText(CancellationToken cancellationToken = default) =>
        SourceText.From(content, Encoding.UTF8);
}

file sealed class AnalyzerConfigOptionsProviderImplementation(IEnumerable<KeyValuePair<string, string>> globalOptions)
    : AnalyzerConfigOptionsProvider
{
    public override AnalyzerConfigOptions GlobalOptions { get; } = new AnalyzerConfigOptionsImplementation(globalOptions);

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) =>
        throw new NotImplementedException();
    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) =>
        throw new NotImplementedException();

    private sealed class AnalyzerConfigOptionsImplementation(IEnumerable<KeyValuePair<string, string>> options)
        : AnalyzerConfigOptions
    {
        public static readonly AnalyzerConfigOptionsImplementation Empty = new([]);

        private readonly Dictionary<string, string> _options = new(options
            .Select(kvp => new KeyValuePair<string, string>($"build_property.{kvp.Key}", kvp.Value)));

        public string this[string key] { get => _options[key]; init => _options[key] = value; }

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            => _options.TryGetValue(key, out value);
    }
}

internal sealed record class ProjectConfig(
    LanguageVersion LanguageVersion,
    Dictionary<string, string> GlobalOptions)
{
    public static readonly ProjectConfig Default = new(LanguageVersion.Default, []);
}

