using System.Text;
using System.Text.Json;
using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Diagnostics;

public static class SourceSpanExtensions
{
    extension(SourceSpan)
    {
        public static SourceSpan FromSourceText(SourceText sourceText, int offset = 0, int length = -1) =>
            new SourceSpan(sourceText, offset, length == -1 ? sourceText.Length - offset : length);

        public static SourceSpan FromException(SourceText sourceText, JsonException exception)
        {
            if (sourceText.IsEmpty) return SourceSpan.FromSourceText(sourceText);
            var lineIndex = (int)Math.Max(0, Math.Min(exception.LineNumber ?? 0, sourceText.Lines.Length - 1));
            var line = sourceText.Lines[lineIndex].SourceSpan;
            var text = sourceText.Text.AsSpan(line.Offset, line.Length);
            var bytePosition = Math.Max(0, exception.BytePositionInLine ?? 0);
            var character = 0;
            long bytes = 0;
            while (character < text.Length)
            {
                var width = char.IsHighSurrogate(text[character]) && character + 1 < text.Length && char.IsLowSurrogate(text[character + 1]) ? 2 : 1;
                var byteWidth = Encoding.UTF8.GetByteCount(text.Slice(character, width));
                if (bytes + byteWidth > bytePosition) break;
                bytes += byteWidth;
                character += width;
            }
            return new SourceSpan(sourceText, line.Offset + character, line.Length - character);
        }
    }
}
