using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace AvroSourceGenerator.Tests.Infrastructure;

public static class VariantGenerationAssertions
{
    public static string Schema(string firstType, string secondType, bool nullable = false) => $$"""
        {
          "type": "record",
          "name": "Envelope",
          "namespace": "Variants",
          "fields": [{
            "name": "choice",
            "type": [
              {{Member(firstType, "First")}},
              {{Member(secondType, "Second")}}{{(nullable ? ", \"null\"" : "")}}
            ]
          }]
        }
        """;

    public static void Verify(
        string library, ImmutableArray<MetadataReference> references,
        string firstType, string secondType, bool nullable, bool supportsVariant, string declaration)
    {
        var input = GeneratorInput.Create(
            [ProjectFile.Schema(Schema(firstType, secondType, nullable))], references,
            new ProjectConfig
            {
                AvroLibrary = library,
                LanguageVersion = LanguageVersion.CSharp12,
                RecordDeclaration = declaration
            });
        var driver = input.GeneratorDriver.RunGeneratorsAndUpdateCompilation(
            input.Compilation, out var compilation, out var diagnostics, TestContext.Current.CancellationToken);
        Assert.Empty(diagnostics);
        Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        var envelope = compilation.GetTypeByMetadataName("Variants.Envelope");
        Assert.NotNull(envelope);
        var field = Assert.IsAssignableFrom<IPropertySymbol>(Assert.Single(envelope.GetMembers("choice")));

        if (supportsVariant)
        {
            var variant = compilation.GetTypeByMetadataName("Variants.IEnvelopeChoiceVariant");
            Assert.NotNull(variant);
            Assert.Equal(TypeKind.Interface, variant.TypeKind);
            Assert.True(SymbolEqualityComparer.Default.Equals(variant, field.Type));
            Assert.Equal(nullable ? NullableAnnotation.Annotated : NullableAnnotation.NotAnnotated, field.NullableAnnotation);
            foreach (var name in new[] { "First", "Second" })
            {
                var member = compilation.GetTypeByMetadataName("Variants." + name);
                Assert.NotNull(member);
                Assert.Contains(member.AllInterfaces, implemented => SymbolEqualityComparer.Default.Equals(variant, implemented));
            }
        }
        else
        {
            Assert.Null(compilation.GetTypeByMetadataName("Variants.IEnvelopeChoiceVariant"));
            Assert.Equal(SpecialType.System_Object, field.Type.SpecialType);
            if (firstType == "fixed") Assert.Null(compilation.GetTypeByMetadataName("Variants.First"));
            if (secondType == "fixed") Assert.Null(compilation.GetTypeByMetadataName("Variants.Second"));
        }

        driver = driver.RunGenerators(input.Compilation, TestContext.Current.CancellationToken);
        Assert.All(
            driver.GetRunResult().Results.Single().TrackedOutputSteps.SelectMany(step => step.Value).SelectMany(step => step.Outputs),
            output => Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));

        if (!supportsVariant) return;

        var changed = new ChangedAdditionalText(input.AdditionalTexts[0].Path, Schema(firstType, "enum", nullable));
        driver = driver.ReplaceAdditionalText(input.AdditionalTexts[0], changed)
            .RunGeneratorsAndUpdateCompilation(input.Compilation, out compilation, out diagnostics, TestContext.Current.CancellationToken);
        Assert.Empty(diagnostics);
        Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Null(compilation.GetTypeByMetadataName("Variants.IEnvelopeChoiceVariant"));
        Assert.DoesNotContain(driver.GetRunResult().Results.Single().GeneratedSources, source => source.HintName == "Variants.IEnvelopeChoiceVariant.Avro.g.cs");
        var changedEnvelope = compilation.GetTypeByMetadataName("Variants.Envelope");
        Assert.NotNull(changedEnvelope);
        Assert.Equal(SpecialType.System_Object, Assert.IsAssignableFrom<IPropertySymbol>(Assert.Single(changedEnvelope.GetMembers("choice"))).Type.SpecialType);
    }

    private static string Member(string type, string name) => type switch
    {
        "fixed" => $$"""{ "type": "fixed", "name": "{{name}}", "size": 16 }""",
        "enum" => $$"""{ "type": "enum", "name": "{{name}}", "symbols": ["A"] }""",
        _ => $$"""{ "type": "{{type}}", "name": "{{name}}", "fields": [] }"""
    };

    private sealed class ChangedAdditionalText(string path, string content) : AdditionalText
    {
        public override string Path => path;
        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(content);
    }
}
