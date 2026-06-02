using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Diagnostics;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Inputs;

internal interface IAvroFile
{
    string Path { get; }
    string? Text { get; }
    ImmutableArray<DiagnosticInfo> Diagnostics { get; }
}

internal static class AvroFile
{
    public static bool IsAvroFile(AdditionalText text) =>
        text.Path.EndsWith(".avsc", StringComparison.OrdinalIgnoreCase) /* ||
        text.Path.EndsWith(".avdl", StringComparison.OrdinalIgnoreCase) */;

    public static IAvroFile FromAdditionalText(AdditionalText additionalText, CancellationToken cancellationToken)
    {
        var path = additionalText.Path;
        var text = additionalText.GetText(cancellationToken)?.ToString();

        return FromFileText(path, text);
    }

    public static IAvroFile FromFileText(string path, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return AvroInvalidFile.Empty(path);
        }

        try
        {
            return path switch
            {
                _ when path.EndsWith(".avsc", StringComparison.OrdinalIgnoreCase) => new AvroSchemaFile(path, text!),
                //_ when path.EndsWith(".avdl", StringComparison.OrdinalIgnoreCase)  => Syntax(path, text),
                _ => throw new InvalidOperationException("Unreachable: Unsupported Avro file type."),
            };
        }
        catch (JsonException ex)
        {
            return AvroInvalidFile.Invalid(path, text, ex);
        }
    }
}
