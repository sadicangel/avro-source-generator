using System.Diagnostics.CodeAnalysis;
using AvroSourceGenerator.AvroIDL.Syntax;

namespace AvroSourceGenerator.AvroIDL.Parsing;

internal record class SyntaxIterator(IReadOnlyList<SyntaxToken> Tokens)
{
    private const int MaxSuccessiveMatchTokenErrors = 1;

    private int _successiveMatchTokenErrors = 0;

    public int Index { get; private set; }

    public SyntaxToken Current { get => Tokens[Math.Min(Tokens.Count - 1, Math.Max(0, Index))]; }

    public SyntaxToken Peek(int offset = 0)
    {
        var index = Index + offset;
        if (index >= Tokens.Count)
            return Tokens[^1];
        return Tokens[index];
    }

    public bool TryMatch([MaybeNullWhen(false)] out SyntaxToken token, params ReadOnlySpan<SyntaxKind> syntaxKinds)
    {
        if (syntaxKinds.Length == 0)
        {
            token = Current;
            ++Index;
            return true;
        }

        foreach (var syntaxKind in syntaxKinds)
        {
            if (syntaxKind == Current.SyntaxKind)
            {
                token = Current;
                ++Index;
                return true;
            }
        }

        token = null;
        return false;
    }

    public SyntaxToken Match(params ReadOnlySpan<SyntaxKind> syntaxKinds)
    {
        if (syntaxKinds.Length == 0)
        {
            var current = Current;
            ++Index;
            return current;
        }

        foreach (var syntaxKind in syntaxKinds)
        {
            if (TryMatch(out var token, syntaxKind))
            {
                _successiveMatchTokenErrors = 0;
                return token;
            }
        }

        if (_successiveMatchTokenErrors++ < MaxSuccessiveMatchTokenErrors)
            Current.SyntaxTree.Diagnostics.ReportUnexpectedToken(syntaxKinds[0], Current);

        var syntheticToken = SyntaxToken.CreateSynthetic(syntaxKinds[0], Current.SyntaxTree, Current.SourceSpanWithTrivia.Offset);

        // Avoid overflowing the stack.
        ++Index;

        return syntheticToken;
    }
}
