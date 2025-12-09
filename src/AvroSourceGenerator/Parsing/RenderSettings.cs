using System.Collections.Immutable;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.Parsing;

internal readonly record struct RenderSettings(
    AvroLibrary AvroLibrary,
    LanguageVersion LanguageVersion,
    LanguageFeatures LanguageFeatures,
    string AccessModifier,
    string RecordDeclaration,
    ImmutableArray<DiagnosticInfo> Diagnostics)
{
    public bool IsValid => !Diagnostics.Any(x => x.Descriptor.DefaultSeverity is Microsoft.CodeAnalysis.DiagnosticSeverity.Error);

    public bool Equals(RenderSettings other) =>
        AvroLibrary == other.AvroLibrary &&
        LanguageVersion == other.LanguageVersion &&
        LanguageFeatures == other.LanguageFeatures &&
        AccessModifier == other.AccessModifier &&
        RecordDeclaration == other.RecordDeclaration &&
        // This will not avoid all cases, but it's good enough for now.
        Diagnostics.OrderBy(x => x.Descriptor.Id).SequenceEqual(other.Diagnostics.OrderBy(x => x.Descriptor.Id));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(AvroLibrary);
        hash.Add(LanguageVersion);
        hash.Add(LanguageFeatures);
        hash.Add(AccessModifier);
        hash.Add(RecordDeclaration);
        foreach (var diagnostic in Diagnostics)
            hash.Add(diagnostic);

        return hash.ToHashCode();
    }
}
