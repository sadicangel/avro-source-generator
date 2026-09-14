using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.Configuration;

internal readonly record struct CompilationEnvironment
{
    private readonly ImmutableArray<AvroLibraryReference> _avroLibraries;

    public CompilationEnvironment(ImmutableArray<AvroLibraryReference> avroLibraries, LanguageVersion languageVersion)
    {
        _avroLibraries = avroLibraries;
        LanguageVersion = languageVersion;
    }

    public ImmutableArray<AvroLibraryReference> AvroLibraries => _avroLibraries.IsDefault ? [] : _avroLibraries;

    public LanguageVersion LanguageVersion { get; }

    public bool Equals(CompilationEnvironment other) =>
        LanguageVersion == other.LanguageVersion && AvroLibraries.SequenceEqual(other.AvroLibraries);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(LanguageVersion);
        foreach (var library in AvroLibraries)
        {
            hash.Add(library);
        }

        return hash.ToHashCode();
    }

    public static CompilationEnvironment FromCompilation(Compilation compilation, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var csharpCompilation = (CSharpCompilation)compilation;

        // Keep canonical order: Apache, then Chr. Equality and hashing rely on it.
        var avroLibraries = ImmutableArray.CreateBuilder<AvroLibraryReference>();

        if (csharpCompilation.GetTypeByMetadataName("Avro.Specific.ISpecificRecord") is not null)
            avroLibraries.Add(AvroLibraryReference.Apache);

        if (csharpCompilation.GetTypeByMetadataName("Chr.Avro.Abstract.Schema") is not null)
            avroLibraries.Add(AvroLibraryReference.Chr);

        return new CompilationEnvironment(avroLibraries.ToImmutable(), csharpCompilation.LanguageVersion);
    }
}
