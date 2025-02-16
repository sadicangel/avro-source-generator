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
    public static SettingsTask Verify(string avro, params string[] sources) =>
        Verifier.Verify(GenerateOutput(sources, [avro]));

    public static GeneratedOutput GenerateOutput(string[] sourceTexts, string[] additionalTexts)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Default);
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
            "generator",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        CSharpGeneratorDriver
            .Create(new AvroSourceGenerator())
            .AddAdditionalTexts([.. additionalTexts.Select(t => new AdditionalTextImplementation(t))])
            .WithUpdatedParseOptions(parseOptions)
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
