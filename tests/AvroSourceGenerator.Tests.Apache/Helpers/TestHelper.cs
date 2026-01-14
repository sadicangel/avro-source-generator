using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Tests.Apache.Helpers;

internal static class TestHelper
{
    public static SettingsTask VerifySourceCode(
        [StringSyntax(StringSyntaxAttribute.Json)]
        string schema,
        string? source = null,
        ProjectConfig config = default,
        [CallerFilePath] string sourceFile = "")
    {
        var input = GeneratorInput.Create(
            sourceTexts: source is null ? [] : [source],
            additionalTexts: [schema],
            executableReferences: [MetadataReference.CreateFromFile(typeof(Avro.Schema).Assembly.Location)],
            projectConfig: config);

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
        string schema,
        string? source = null,
        ProjectConfig config = default,
        [CallerFilePath] string sourceFile = "")
    {
        var input = GeneratorInput.Create(
            sourceTexts: source is null ? [] : [source],
            additionalTexts: [schema],
            executableReferences: [MetadataReference.CreateFromFile(typeof(Avro.Schema).Assembly.Location)],
            projectConfig: config);

        var (diagnostics, _) = GeneratorOutput.Create(input);

        return Verify(Assert.Single(diagnostics), sourceFile: sourceFile);
    }
}
