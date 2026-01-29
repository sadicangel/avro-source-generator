using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Tests.Chr.Helpers;

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
            executableReferences:
            [
                MetadataReference.CreateFromFile(typeof(global::Chr.Avro.Abstract.Schema).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(global::Chr.Avro.Serialization.IBinarySerializerBuilder).Assembly.Location),
            ],
            projectConfig: config);

        var (diagnostics, documents) = GeneratorOutput.Create(input);

        // Ignore assembly reference mismatches because Chr.Avro references .NET 6, while we're targeting .NET 10.
        diagnostics = diagnostics.RemoveAll(d => d.Descriptor.Id is "CS1701");

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
            executableReferences:
            [
                MetadataReference.CreateFromFile(typeof(global::Chr.Avro.Abstract.Schema).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(global::Chr.Avro.Serialization.IBinarySerializerBuilder).Assembly.Location),
            ],
            projectConfig: config);

        var (diagnostics, _) = GeneratorOutput.Create(input);

        // Ignore assembly reference mismatches because Chr.Avro references .NET 6, while we're targeting .NET 10.
        diagnostics = diagnostics.RemoveAll(d => d.Descriptor.Id is "CS1701");

        return Verify(Assert.Single(diagnostics), sourceFile: sourceFile);
    }
}
