using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace AvroSourceGenerator.Tests.Helpers;

internal static class TestHelper
{
    public static SettingsTask VerifySourceCode(
        [StringSyntax(StringSyntaxAttribute.Json)]
        string schema,
        string? source = null,
        ProjectConfig config = default)
    {
        var input = GeneratorInput.Create(
            sourceTexts: source is null ? [] : [source],
            additionalTexts: [schema],
            executableReferences: [],
            projectConfig: config with { AvroLibrary = "None" });

        var (diagnostics, documents) = GeneratorOutput.Create(input);

        if (diagnostics.Length > 0)
        {
            Assert.Fail(
                string.Join(
                    Environment.NewLine,
                    diagnostics.Select(d => $"{d.Id}: {d.GetMessage(CultureInfo.InvariantCulture)}")));
        }

        return Verify(documents.Select(document => new Target("txt", document.Content)));
    }

    public static SettingsTask VerifyDiagnostic(
        string schema,
        string? source = null,
        ProjectConfig config = default)
    {
        var input = GeneratorInput.Create(
            sourceTexts: source is null ? [] : [source],
            additionalTexts: [schema],
            executableReferences: [],
            projectConfig: config with { AvroLibrary = "None" });

        var (diagnostics, _) = GeneratorOutput.Create(input);

        return Verify(Assert.Single(diagnostics));
    }
}
