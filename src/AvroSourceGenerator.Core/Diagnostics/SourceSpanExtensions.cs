using AvroSourceGenerator.Compiler;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Diagnostics;

public static class SourceSpanExtensions
{
    extension(SourceSpan)
    {
        public static SourceSpan FromSourceText(SourceText sourceText, int offset = 0, int length = -1) =>
            new SourceSpan(sourceText, offset, length == -1 ? sourceText.Length - offset : length);

        public static SourceSpan FromSourceFile(ISourceFile sourceFile, int offset = 0, int length = -1)
        {
            ArgumentNullException.ThrowIfNull(sourceFile);
            return SourceSpan.FromSourceText(sourceFile.Text, offset, length);
        }
    }
}
