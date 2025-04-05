using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
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
    public static SettingsTask VerifySourceCode(
        string schema,
        string? source = null,
        ProjectConfig? config = null)
    {
        var (diagnostics, documents) = GenerateOutput(
            source is null ? [] : [source],
            [schema],
            config);

        if (diagnostics.Length > 0)
        {
            Assert.Fail(string.Join(
                Environment.NewLine,
                diagnostics.Select(d => $"{d.Id}: {d.GetMessage(CultureInfo.InvariantCulture)}")));
        }

        return Verifier.Verify(documents.Select(document => new Target("txt", document.Content)));
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
            .Concat([
                MetadataReference.CreateFromFile(typeof(AvroSourceGenerator).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(AvroAttribute).Assembly.Location),
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

        var analyzerConfigOptions = new AnalyzerConfigOptionsProviderImplementation(projectConfig.GlobalOptions);

        CSharpGeneratorDriver
            .Create(new AvroSourceGenerator())
            .AddAdditionalTexts([.. additionalTexts.Select(t => new AdditionalTextImplementation(t))])
            .WithUpdatedParseOptions(parseOptions)
            .WithUpdatedAnalyzerConfigOptions(analyzerConfigOptions)
            .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var compilationWithAnalyzers = outputCompilation
            .WithAnalyzers(DiagnosticAnalyzers.Analyzers, new AnalyzerOptions([], analyzerConfigOptions));

        diagnostics = diagnostics.AddRange(compilationWithAnalyzers
            .GetAllDiagnosticsAsync().GetAwaiter().GetResult()
            .RemoveAll(x => x.DefaultSeverity < DiagnosticSeverity.Warning));

        var documents = outputCompilation.SyntaxTrees
            .Where(st => !string.IsNullOrEmpty(st.FilePath))
            .Select(st => new Document(st.FilePath.Replace('\\', '/'), st.ToString()))
            .ToImmutableArray();

        return new(diagnostics, documents);
    }
}

file static class DiagnosticAnalyzers
{
    public static ImmutableArray<DiagnosticAnalyzer> Analyzers { get; }

    static DiagnosticAnalyzers()
    {
        var folder = GetAnalizersFolder("8");

        var asmLoader = new AnalyzerAssemblyLoaderImplementation();
        var analyzerReferences = Directory.EnumerateFiles(folder, "*.dll")
            .Select(dll => new AnalyzerFileReference(dll, asmLoader))
            .SelectMany(x => x.GetAnalyzers(LanguageNames.CSharp))
            .ToImmutableArray();

        Analyzers = analyzerReferences;
    }

    private static string GetAnalizersFolder(string sdkHint)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "--list-sdks",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        var sdks = output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line =>
            {
                var parts = line.Split(['[', ']'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return (Version: parts[0], Path: Path.Combine(parts[1], parts[0]));
            })
            .OrderBy(x => x.Version)
            .ToList();

        var sdk = sdks.Find(x => x.Version.StartsWith(sdkHint)).Path ?? sdks[^1].Path;

        return Path.Combine(sdk, "Sdks", "Microsoft.NET.Sdk", "analyzers");
    }

    private sealed class AnalyzerAssemblyLoaderImplementation : IAnalyzerAssemblyLoader
    {
        public void AddDependencyLocation(string fullPath) { }
        public Assembly LoadFromPath(string fullPath) => Assembly.LoadFrom(fullPath);
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

internal sealed record class ProjectConfig(
    LanguageVersion LanguageVersion,
    Dictionary<string, string> GlobalOptions)
{
    public static readonly ProjectConfig Default = new(LanguageVersion.Default, []);
}

