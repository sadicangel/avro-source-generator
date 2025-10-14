using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AvroSourceGenerator.Tests.Setup;

public readonly record struct GeneratorOutput(ImmutableArray<Diagnostic> Diagnostics, ImmutableArray<Document> Documents)
{
    public static GeneratorOutput Create(GeneratorInput generatorInput)
    {
        var (parseOptions, optionsProvider, compilation, generatorDriver) = generatorInput;

        generatorDriver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out var diagnostics);

        var analyzerDiagnostics = compilation
            .WithAnalyzers(DiagnosticAnalyzers.Analyzers, new AnalyzerOptions([], optionsProvider))
            .GetAllDiagnosticsAsync().GetAwaiter().GetResult()
            .RemoveAll(x => x.DefaultSeverity < DiagnosticSeverity.Warning);

        diagnostics = diagnostics.AddRange(analyzerDiagnostics);

        var documents = compilation.SyntaxTrees
            .Where(st => !string.IsNullOrEmpty(st.FilePath))
            .Select(st => new Document(st.FilePath.Replace('\\', '/'), st.ToString()))
            .ToImmutableArray();

        return new(diagnostics, documents);
    }
}
