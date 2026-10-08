using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.UnitTests.Chr;

public sealed class UnionTaggedGenerationTests
{
    [Theory]
    [InlineData("fixed", "error")]
    [InlineData("record", "error")]
    public void Substituted_and_error_members_generate_compilable_tagged_unions(string firstType, string secondType)
    {
        var input = GeneratorInput.Create(
            [ProjectFile.Schema(VariantGenerationAssertions.Schema(firstType, secondType))],
            Snapshot.References,
            new ProjectConfig(LanguageVersion.Preview) { LanguageFeatures = "CSharp15", PreviewFeatures = "Unions" });
        input.GeneratorDriver.RunGeneratorsAndUpdateCompilation(
            input.Compilation, out var compilation, out var diagnostics, TestContext.Current.CancellationToken);

        Assert.Empty(diagnostics);
        Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error));
    }
}
