using System.Collections.Immutable;
using AvroSourceGenerator.Diagnostics;
using AvroSourceGenerator.Templating;
using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.Configuration;

internal readonly record struct GeneratorConfig(
    TargetProfile TargetProfile,
    LanguageFeatures LanguageFeatures,
    AccessModifier AccessModifier,
    ReferenceResolution ReferenceResolution,
    DuplicateResolution DuplicateResolution,
    ImmutableArray<DiagnosticInfo> Diagnostics)
{
    public bool IsValid => !Diagnostics.Any(x => x.Descriptor.DefaultSeverity is Microsoft.CodeAnalysis.DiagnosticSeverity.Error);

    public bool Equals(GeneratorConfig other) =>
        TargetProfile == other.TargetProfile &&
        LanguageFeatures == other.LanguageFeatures &&
        AccessModifier == other.AccessModifier &&
        ReferenceResolution == other.ReferenceResolution &&
        DuplicateResolution == other.DuplicateResolution &&
        // This will not avoid all cases, but it's good enough for now.
        Diagnostics.OrderBy(x => x.Descriptor.Id).SequenceEqual(other.Diagnostics.OrderBy(x => x.Descriptor.Id));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(TargetProfile);
        hash.Add(LanguageFeatures);
        hash.Add(AccessModifier);
        hash.Add(ReferenceResolution);
        hash.Add(DuplicateResolution);
        foreach (var diagnostic in Diagnostics)
            hash.Add(diagnostic);

        return hash.ToHashCode();
    }

    public static GeneratorConfig FromEnvironment((ProjectSettings, CompilationInfo) input, CancellationToken cancellationToken)
    {
        var (projectSettings, compilationInfo) = input;

        var targetProfile = GetTargetProfile(projectSettings.AvroLibrary ?? AvroLibrary.Auto, compilationInfo.LanguageVersion, compilationInfo.AvroLibraries, out var diagnostics);
        var languageFeatures = GetLanguageFeatures(projectSettings.LanguageFeatures ?? MapVersionToFeatures(compilationInfo.LanguageVersion), targetProfile, projectSettings.RecordDeclaration);
        var accessModifier = projectSettings.AccessModifier ?? AccessModifier.Public;
        var referenceResolution = projectSettings.ReferenceResolution ?? ReferenceResolution.Strict;
        var duplicateResolution = projectSettings.DuplicateResolution ?? DuplicateResolution.Error;

        return new GeneratorConfig(targetProfile, languageFeatures, accessModifier, referenceResolution, duplicateResolution, diagnostics);
    }

    private static TargetProfile GetTargetProfile(AvroLibrary avroLibrary, LanguageVersion languageVersion, ImmutableArray<AvroLibraryReference> references, out ImmutableArray<DiagnosticInfo> diagnostics)
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
                    diagnostics = [NoAvroLibraryDetectedDiagnostic.Create(LocationInfo.None)];
                    avroLibrary = AvroLibrary.None;
                    break;

                default:
                    diagnostics = [MultipleAvroLibrariesDetectedDiagnostic.Create(LocationInfo.None, references)];
                    avroLibrary = AvroLibrary.None;
                    break;
            }
        }

        return avroLibrary switch
        {
            AvroLibrary.Apache => TargetProfile.Apache,
            AvroLibrary.Chr => TargetProfile.Chr,
            _ when languageVersion < LanguageVersion.CSharp10 => TargetProfile.Legacy,
            _ => TargetProfile.Modern,
        };
    }

    private static LanguageFeatures GetLanguageFeatures(LanguageFeatures languageFeatures, TargetProfile targetProfile, string? recordDeclaration)
    {
        var useRecords = targetProfile is not TargetProfile.Legacy && recordDeclaration switch
        {
            "record" => true,
            "class" => false,
            _ => languageFeatures.HasFlag(LanguageFeatures.Records)
        };

        return useRecords
            ? languageFeatures | LanguageFeatures.Records
            : languageFeatures & ~LanguageFeatures.Records;
    }

    private static LanguageFeatures MapVersionToFeatures(LanguageVersion languageVersion)
    {
        return languageVersion switch
        {
            <= LanguageVersion.CSharp7_3 => LanguageFeatures.CSharp7_3,
            LanguageVersion.CSharp8 => LanguageFeatures.CSharp8,
            LanguageVersion.CSharp9 => LanguageFeatures.CSharp9,
            LanguageVersion.CSharp10 => LanguageFeatures.CSharp10,
            LanguageVersion.CSharp11 => LanguageFeatures.CSharp11,
            LanguageVersion.CSharp12 => LanguageFeatures.CSharp12,
            //LanguageVersion.CSharp13 => LanguageFeatures.CSharp13,
            //LanguageVersion.CSharp14 => LanguageFeatures.CSharp14,
            _ => LanguageFeatures.Latest,
        };
    }
}
