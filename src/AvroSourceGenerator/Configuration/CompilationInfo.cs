using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
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

    public static CompilationInfo FromCompilation(Compilation compilation, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var csharpCompilation = (CSharpCompilation)compilation;

        var avroLibraries = ImmutableArray.CreateBuilder<AvroLibraryReference>();

        if (csharpCompilation.GetTypeByMetadataName("Avro.Specific.ISpecificRecord") is not null)
            avroLibraries.Add(AvroLibraryReference.Apache);

        if (csharpCompilation.GetTypeByMetadataName("Chr.Avro.Abstract.Schema") is not null)
            avroLibraries.Add(AvroLibraryReference.Chr);

        return new CompilationInfo(avroLibraries.ToImmutable(), csharpCompilation.LanguageVersion);
    }
}
