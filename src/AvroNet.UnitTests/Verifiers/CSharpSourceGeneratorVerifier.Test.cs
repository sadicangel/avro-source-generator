using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace AvroNet.UnitTests.Verifiers;

internal static partial class CSharpSourceGeneratorVerifier<TSourceGenerator>
    where TSourceGenerator : IIncrementalGenerator, new()
{
    public sealed class Test : CSharpSourceGeneratorTest<EmptySourceGeneratorProvider, MSTestVerifier>
    {
        public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.Default;

        protected override IEnumerable<ISourceGenerator> GetSourceGenerators() => [new TSourceGenerator().AsSourceGenerator()];

        protected override CompilationOptions CreateCompilationOptions()
        {
            var compilationOptions = base.CreateCompilationOptions();
            return compilationOptions.WithSpecificDiagnosticOptions(
                compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
        }

        protected override async Task<Compilation> GetProjectCompilationAsync(Project project, IVerifier verifier, CancellationToken cancellationToken)
        {
            var compilation = await base.GetProjectCompilationAsync(project, verifier, cancellationToken);
            return compilation;
        }

        protected override ParseOptions CreateParseOptions()
        {
            return ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(LanguageVersion);
        }
    }
}
