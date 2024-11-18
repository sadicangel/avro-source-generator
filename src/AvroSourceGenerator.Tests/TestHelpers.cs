using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace AvroSourceGenerator.Tests;

public readonly record struct Document(string FileName, string Content);
public readonly record struct GeneratedOutput(ImmutableArray<Document> Documents, ImmutableArray<Diagnostic> Diagnostics);

internal static class TestHelpers
{
    public static LanguageFeatures LanguageFeatures =>
#if NET472 || NET48 || NETSTANDARD2_0
        LanguageFeatures.CSharp7_3;
#elif NETSTANDARD2_1
        LanguageFeatures.CSharp8;
#elif NET5_0
        LanguageFeatures.CSharp9;
#elif NET6_0
        LanguageFeatures.CSharp10;
#elif NET7_0
        LanguageFeatures.CSharp11;
#elif NET8_0
        LanguageFeatures.CSharp12;
#else
        LanguageFeatures.None;
#endif

    private static readonly DefaultVerifier s_verifier = new();

    public static void Verify(string source, params Document[] expectedDocuments)
    {
        var (actualDocuments, actualDiagnostics) = GetGeneratedOutput([source]);

        Assert.Empty(actualDiagnostics);

        VerifyDocuments(
            [.. actualDocuments.Select(doc => doc with { FileName = Path.GetFileName(doc.FileName) })],
            [.. expectedDocuments]);
    }

    public static GeneratedOutput GetGeneratedOutput(IEnumerable<string> sources)
    {
        var syntaxTrees = sources.Select(source => CSharpSyntaxTree.ParseText(source));
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(_ => !_.IsDynamic && !string.IsNullOrWhiteSpace(_.Location))
            .Select(_ => MetadataReference.CreateFromFile(_.Location))
            .Concat([MetadataReference.CreateFromFile(typeof(AvroSourceGenerator).Assembly.Location)]);

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

        return new(documents, diagnostics);
    }

    public static void VerifyDocuments(
        ImmutableArray<Document> actualDocuments,
        ImmutableArray<Document> expectedDocuments)
    {
        actualDocuments = actualDocuments.Sort((a, b) => string.CompareOrdinal(a.FileName, b.FileName));
        expectedDocuments = expectedDocuments.Sort((a, b) => string.CompareOrdinal(a.FileName, b.FileName));

        // Use EqualOrDiff to verify the actual and expected filenames (and total collection length) in a convenient manner
        s_verifier.EqualOrDiff(
            string.Join(Environment.NewLine, expectedDocuments.Select(doc => doc.FileName)),
            string.Join(Environment.NewLine, actualDocuments.Select(doc => doc.FileName)),
            $"Expected source file list to match");

        // Follow by verifying each property of interest
        foreach (var (expected, actual) in Zip(expectedDocuments, actualDocuments))
        {
            s_verifier.Equal(
                expected.FileName,
                actual.FileName,
                $"Filename was expected to be '{expected.FileName}' but was '{actual.FileName}'");

            s_verifier.EqualOrDiff(
                expected.Content,
                actual.Content,
                $"Content of '{expected.FileName}' did not match. Diff shown with expected as baseline:");
        }

        static IEnumerable<(Document Expected, Document Actual)> Zip(ImmutableArray<Document> expected, ImmutableArray<Document> actual)
        {
            Debug.Assert(expected.Length == actual.Length);
            for (var i = 0; i < expected.Length; ++i)
                yield return (expected[i], actual[i]);
        }
    }
}

