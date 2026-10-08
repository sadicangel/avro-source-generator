using System.Collections.Immutable;
using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Templating;
using AvroSourceGenerator.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.Configuration;

internal sealed record GeneratorConfiguration(
    GenerationTarget GenerationTarget,
    LanguageFeatures LanguageFeatures,
    AccessModifier AccessModifier,
    ReferenceResolution ReferenceResolution,
    DuplicateResolution DuplicateResolution,
    ImmutableArray<AvroDiagnostic> Diagnostics)
{
    public bool Equals(GeneratorConfiguration? other) =>
        other is not null &&
        GenerationTarget == other.GenerationTarget &&
        LanguageFeatures == other.LanguageFeatures &&
        AccessModifier == other.AccessModifier &&
        ReferenceResolution == other.ReferenceResolution &&
        DuplicateResolution == other.DuplicateResolution &&
        Diagnostics.SequenceEqual(other.Diagnostics);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(GenerationTarget);
        hash.Add(LanguageFeatures);
        hash.Add(AccessModifier);
        hash.Add(ReferenceResolution);
        hash.Add(DuplicateResolution);
        foreach (var diagnostic in Diagnostics)
            hash.Add(diagnostic);

        return hash.ToHashCode();
    }

    public static GeneratorConfiguration Resolve((ProjectProperties, CompilationEnvironment) input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (projectProperties, compilationEnvironment) = input;
        var generationTarget = GetGenerationTarget(projectProperties.AvroLibrary, compilationEnvironment.LanguageVersion, compilationEnvironment.AvroLibraries, out var diagnostics);
        var languageFeatures = GetLanguageFeatures(projectProperties, generationTarget) & MapVersionToFeatures(compilationEnvironment.LanguageVersion);
        return new GeneratorConfiguration(generationTarget, languageFeatures, projectProperties.AccessModifier, projectProperties.ReferenceResolution, projectProperties.DuplicateResolution, diagnostics);
    }

    private static GenerationTarget GetGenerationTarget(AvroLibrary avroLibrary, LanguageVersion languageVersion, ImmutableArray<AvroLibraryReference> references, out ImmutableArray<AvroDiagnostic> diagnostics)
    {
        diagnostics = [];
        if (avroLibrary is AvroLibrary.Auto)
        {
            switch (references)
            {
                case [var reference]:
                    diagnostics = [];
                    avroLibrary = reference.ToAvroLibrary();
                    break;

                case []:
                    diagnostics = [AvroDiagnostic.NoAvroLibraryDetected(SourceSpan.None)];
                    avroLibrary = AvroLibrary.None;
                    break;

                default:
                    diagnostics = [AvroDiagnostic.MultipleAvroLibrariesDetected(SourceSpan.None, references)];
                    avroLibrary = AvroLibrary.None;
                    break;
            }
        }

        return avroLibrary switch
        {
            AvroLibrary.Apache => GenerationTarget.Apache,
            AvroLibrary.Chr => GenerationTarget.Chr,
            _ when languageVersion < LanguageVersion.CSharp10 => GenerationTarget.Legacy,
            _ => GenerationTarget.Modern,
        };
    }

    private static LanguageFeatures GetLanguageFeatures(ProjectProperties projectProperties, GenerationTarget generationTarget) => projectProperties.LanguageFeatures
        .SetRecords(projectProperties.LanguageFeatures.HasRecords && generationTarget is not GenerationTarget.Legacy && projectProperties.RecordDeclaration is "record")
        .SetUnions(projectProperties.LanguageFeatures.HasUnions && (projectProperties.PreviewFeatures & PreviewFeatures.Unions) != 0);

    private static LanguageFeatures MapVersionToFeatures(LanguageVersion languageVersion) => languageVersion switch
    {
        <= LanguageVersion.CSharp7_3 => LanguageFeatures.CSharp7_3,
        LanguageVersion.CSharp8 => LanguageFeatures.CSharp8,
        LanguageVersion.CSharp9 => LanguageFeatures.CSharp9,
        LanguageVersion.CSharp10 => LanguageFeatures.CSharp10,
        LanguageVersion.CSharp11 => LanguageFeatures.CSharp11,
        LanguageVersion.CSharp12 => LanguageFeatures.CSharp12,
        // Numeric values keep this mapping compatible with the Roslyn 5.0 baseline.
        <= (LanguageVersion)1300 => LanguageFeatures.CSharp13,
        <= (LanguageVersion)1400 => LanguageFeatures.CSharp14,
        <= (LanguageVersion)1500 => LanguageFeatures.CSharp15,
        LanguageVersion.Preview => LanguageFeatures.CSharp15,
        _ => LanguageFeatures.All,
    };
}
