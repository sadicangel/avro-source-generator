using System.Collections.Immutable;
using AvroSourceGenerator.AvroIDL.Syntax;

namespace AvroSourceGenerator.AvroIDL.Parsing;

partial class Parser
{
    private static SyntaxList<TNode> ParseSyntaxList<TNode>(
        SyntaxTree syntaxTree,
        SyntaxIterator iterator,
        ReadOnlySpan<SyntaxKind> endingKinds,
        ParseFunc<TNode> parseNode)
        where TNode : SyntaxNode
    {
        var nodes = ImmutableArray.CreateBuilder<TNode>();

        ParseSyntaxList(syntaxTree, iterator, endingKinds, parseNode, nodes.Add);

        return new SyntaxList<TNode>(nodes.ToImmutable());
    }

    private static void ParseSyntaxList<TNode>(
        SyntaxTree syntaxTree,
        SyntaxIterator iterator,
        ReadOnlySpan<SyntaxKind> endingKinds,
        ParseFunc<TNode> parseNode,
        Action<TNode> nodeParsed)
        where TNode : SyntaxNode
    {
        while (!iterator.Current.SyntaxKind.IsEndingKind(endingKinds))
        {
            var start = iterator.Current;

            nodeParsed(parseNode(syntaxTree, iterator));

            // No tokens consumed. Skip the current token to avoid infinite loop.
            // No need to report any extra error as parse methods already failed.
            if (iterator.Current == start)
                _ = iterator.Match();
        }
    }

    private static SyntaxList<TNode> ParseSyntaxList<TNode>(
        SyntaxTree syntaxTree,
        SyntaxIterator iterator,
        ParseOptionalFunc<TNode> parseNode)
        where TNode : SyntaxNode
    {
        var nodes = ImmutableArray.CreateBuilder<TNode>();

        while (!iterator.Current.SyntaxKind.IsEndingKind())
        {
            var start = iterator.Current;

            var node = parseNode(syntaxTree, iterator);
            if (node is null)
            {
                break;
            }

            nodes.Add(node);

            // No tokens consumed. Skip the current token to avoid infinite loop.
            // No need to report any extra error as parse methods already failed.
            if (iterator.Current == start)
                _ = iterator.Match();
        }

        return new SyntaxList<TNode>(nodes.ToImmutable());
    }

    private static SeparatedSyntaxList<TNode> ParseSyntaxList<TNode>(
        SyntaxTree syntaxTree,
        SyntaxIterator iterator,
        SyntaxKind separatorKind,
        ReadOnlySpan<SyntaxKind> endingKinds,
        ParseFunc<TNode> parseNode)
        where TNode : SyntaxNode
    {
        var nodes = ImmutableArray.CreateBuilder<SyntaxNode>();

        ParseSyntaxList(syntaxTree, iterator, separatorKind, endingKinds, parseNode, nodes.Add);

        return new SeparatedSyntaxList<TNode>(nodes.ToImmutable());
    }

    private static void ParseSyntaxList<TNode>(
        SyntaxTree syntaxTree,
        SyntaxIterator iterator,
        SyntaxKind separatorKind,
        ReadOnlySpan<SyntaxKind> endingKinds,
        ParseFunc<TNode> parseNode,
        Action<SyntaxNode> nodeParsed)
        where TNode : SyntaxNode
    {
        var parseNext = true;
        while (parseNext && !iterator.Current.SyntaxKind.IsEndingKind(endingKinds))
        {
            var start = iterator.Current;

            nodeParsed(parseNode(syntaxTree, iterator));

            if (iterator.TryMatch(out var separatorToken, separatorKind))
                nodeParsed(separatorToken);
            else
                parseNext = false;

            // No tokens consumed. Skip the current token to avoid infinite loop.
            // No need to report any extra error as parse methods already failed.
            if (iterator.Current == start)
                _ = iterator.Match();
        }
    }
}
