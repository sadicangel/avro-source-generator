using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace AvroSourceGenerator.Tests.Helpers;

internal static class TestHelper
{
    public static SettingsTask VerifySourceCode(
        [StringSyntax(StringSyntaxAttribute.Json)]
        string schema,
        ProjectConfig config = default,
        [CallerFilePath] string sourceFile = "") => VerifySourceCode([schema], config, sourceFile);

    public static SettingsTask VerifySourceCode(ImmutableArray<string> schemas, ProjectConfig config = default, [CallerFilePath] string sourceFile = "")
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

        return Verify(documents.Select(document => new Target("txt", document.Content)), sourceFile: sourceFile);
    }

    public static SettingsTask VerifyDiagnostic(
        [StringSyntax(StringSyntaxAttribute.Json)]
        string schema,
        ProjectConfig config = default,
        [CallerFilePath] string sourceFile = "") => VerifyDiagnostic([schema], config, sourceFile);

    public static SettingsTask VerifyDiagnostic(
        ImmutableArray<string> schemas,
        ProjectConfig config = default,
        [CallerFilePath] string sourceFile = "")
    {
        var input = GeneratorInput.Create(
            sourceTexts: [],
            additionalTexts: schemas,
            executableReferences: [],
            projectConfig: config with { AvroLibrary = "None" });

        var (diagnostics, _) = GeneratorOutput.Create(input);

        return Verify(Assert.Single(diagnostics), sourceFile: sourceFile);
    }
}
