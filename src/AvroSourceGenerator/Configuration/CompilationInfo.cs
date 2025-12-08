using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.Configuration;

internal readonly record struct CompilationInfo(ImmutableArray<AvroLibraryReference> AvroLibraries, LanguageVersion LanguageVersion)
{
    public bool Equals(CompilationInfo other) =>
        LanguageVersion == other.LanguageVersion && AvroLibraries.OrderBy(x => x).SequenceEqual(other.AvroLibraries.OrderBy(x => x));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(LanguageVersion);
        foreach (var library in AvroLibraries.OrderBy(x => x))
        {
            hash.Add(library);
        }

        return hash.ToHashCode();
    }
}
