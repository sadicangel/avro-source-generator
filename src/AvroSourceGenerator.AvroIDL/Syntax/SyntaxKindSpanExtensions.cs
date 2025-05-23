namespace AvroSourceGenerator.AvroIDL.Syntax;

internal static class SyntaxKindSpanExtensions
{
    extension(SyntaxKind kind)
    {
        public bool IsEndingKind(params ReadOnlySpan<SyntaxKind> endingKinds)
        {
            if (kind is SyntaxKind.EofToken)
                return true;

            for (var i = 0; i < endingKinds.Length; ++i)
            {
                if (endingKinds[i] == kind)
                    return true;
            }

            return false;
        }
    }
}
