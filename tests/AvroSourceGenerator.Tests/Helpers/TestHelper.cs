using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace AvroSourceGenerator.Tests.Helpers;

internal static class TestHelper
{
    public static SettingsTask VerifySourceCode([StringSyntax(StringSyntaxAttribute.Json)] string schema, ProjectConfig config = default) =>
        VerifySourceCode([schema], config);

    public static SettingsTask VerifySourceCode(ImmutableArray<string> schemas, ProjectConfig config = default)
    {
        var input = GeneratorInput.Create(
            sourceTexts: [],
            additionalTexts: schemas,
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

    public static SettingsTask VerifyDiagnostic([StringSyntax(StringSyntaxAttribute.Json)] string schema, ProjectConfig config = default) =>
        VerifyDiagnostic([schema], config);

    public static SettingsTask VerifyDiagnostic(ImmutableArray<string> schemas, ProjectConfig config = default)
    {
        var input = GeneratorInput.Create(
            sourceTexts: [],
            additionalTexts: schemas,
            executableReferences: [],
            projectConfig: config with { AvroLibrary = "None" });

        var (diagnostics, _) = GeneratorOutput.Create(input);

        return Verify(Assert.Single(diagnostics));
    }
}
