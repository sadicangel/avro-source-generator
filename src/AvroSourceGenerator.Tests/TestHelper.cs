using System.CodeDom.Compiler;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.Tests;

public readonly record struct Document(string FileName, string Content);
public readonly record struct GeneratedOutput(ImmutableArray<Diagnostic> Diagnostics, ImmutableArray<Document> Documents);

internal static class TestHelper
{
    public static SettingsTask Verify(params string[] sources) => Verifier.Verify(GenerateOutput(sources));

    public static GeneratedOutput GenerateOutput(params string[] sources)
    {
        var syntaxTrees = sources.Select(source => CSharpSyntaxTree.ParseText(source));
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
            "generator",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        CSharpGeneratorDriver
            .Create(new AvroSourceGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var documents = outputCompilation.SyntaxTrees
            .Where(st => !string.IsNullOrEmpty(st.FilePath))
            .Select(st => new Document(st.FilePath, st.ToString()))
            .ToImmutableArray();

        return new(diagnostics, documents);
    }
}

