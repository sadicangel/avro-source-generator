using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Tests.Helpers;

internal static class TestData
{
    public static TheoryData<string> GetLanguageVersions() =>
        [.. Enum.GetNames<LanguageFeatures>().Where(n => n.StartsWith("CSharp"))];
}
