using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.Tests.Helpers;

internal sealed record class ProjectConfig(
    LanguageVersion LanguageVersion,
    Dictionary<string, string> GlobalOptions)
{
    public static readonly ProjectConfig Default = new(LanguageVersion.Default, []);
}
