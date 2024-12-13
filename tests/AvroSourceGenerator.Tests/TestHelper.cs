using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace AvroSourceGenerator.Tests;

public readonly record struct Document(string FileName, string Content);
public readonly record struct GeneratedOutput(ImmutableArray<Diagnostic> Diagnostics, ImmutableArray<Document> Documents);

internal static class TestHelper
{
    public static SettingsTask Verify(string source) =>
        Verifier.Verify(GenerateOutput([source], []));
    public static SettingsTask VerifyText(string text) =>
        Verifier.Verify(GenerateOutput([], [text]));

    public static GeneratedOutput GenerateOutput(string[] sources, string[] texts)
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
            .AddAdditionalTexts(texts.Select(t => new AdditionalTextImplementation(t)).ToImmutableArray<AdditionalText>())
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
    public override string Path { get; } = $"{Guid.NewGuid()}.avsc";

    public override SourceText? GetText(CancellationToken cancellationToken = default) =>
        SourceText.From(content, Encoding.UTF8);
}
