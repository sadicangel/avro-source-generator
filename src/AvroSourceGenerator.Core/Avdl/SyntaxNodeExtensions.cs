using AvroSourceGenerator.Text;

namespace AvroSourceGenerator.Avdl;

internal static class SyntaxNodeExtensions
{
    extension(ISyntaxNode syntax)
    {
        public SourceSpan GetSourceSpan()
        {
            if (syntax is SyntaxToken token)
                return token.SourceSpan;

            var first = SourceSpan.None;
            var last = SourceSpan.None;
            foreach (var child in syntax.Children())
            {
                var childSpan = child.GetSourceSpan();
                if (childSpan.IsNone)
                    continue;

                if (first.IsNone)
                    first = childSpan;
                else if (!ReferenceEquals(first.SourceText, childSpan.SourceText))
                    return SourceSpan.None;

                last = childSpan;
            }

            if (first.IsNone)
                return SourceSpan.None;

            var end = last.Offset + last.Length;
            return end < first.Offset
                ? SourceSpan.None
                : new SourceSpan(first.SourceText, first.Offset, end - first.Offset);
        }
    }
}
