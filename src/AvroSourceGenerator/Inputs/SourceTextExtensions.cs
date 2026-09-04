using AvroSourceGenerator.Text;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator.Inputs;

public static class SourceTextExtensions
{
    extension(SourceText)
    {
        public static bool IsAvroFile(AdditionalText text) =>
            text.Path.EndsWith(".avsc", StringComparison.OrdinalIgnoreCase) ||
            text.Path.EndsWith(".avpr", StringComparison.OrdinalIgnoreCase) ||
            text.Path.EndsWith(".avdl", StringComparison.OrdinalIgnoreCase);

        public static SourceText FromAdditionalText(AdditionalText additionalText, CancellationToken cancellationToken)
        {
            var path = additionalText.Path;
            var text = additionalText.GetText(cancellationToken)?.ToString() ?? string.Empty;
            return new SourceText(path, text);
        }
    }
}
