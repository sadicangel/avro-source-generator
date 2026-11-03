using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.UnitTests;

public sealed class DefaultPreservationTests
{
    [Theory]
    [InlineData(LanguageVersion.CSharp7_3)]
    [InlineData(LanguageVersion.CSharp12)]
    [InlineData(LanguageVersion.Preview)]
    public void Schema_defaults_do_not_initialize_csharp_properties(LanguageVersion version) =>
        DefaultPreservationAssertions.Verify(version, "None", []);
}
