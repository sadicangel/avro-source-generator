using AvroSourceGenerator.Compiler;

namespace AvroSourceGenerator.Extensions;

public static class AvroFileExtensions
{
    extension(AvroFile)
    {
        public static AvroFile Parse((Text.SourceText SourceText, AvroParseOptions ParseOptions) input, CancellationToken cancellationToken) =>
            AvroFile.Parse(input.SourceText, input.ParseOptions, cancellationToken);
    }
}
