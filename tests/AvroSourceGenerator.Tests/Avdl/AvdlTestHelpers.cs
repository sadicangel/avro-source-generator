using AvroSourceGenerator.Avdl.Syntax;
using AvroSourceGenerator.Avdl.Text;

namespace AvroSourceGenerator.Tests.Avdl;

internal static class AvdlTestHelpers
{
    public static SourceText SourceText(string text) => new SourceText("test.avdl", text);

    public static IReadOnlyList<ISyntaxNode> Flatten(ISyntaxNode node)
    {
        var nodes = new List<ISyntaxNode>();
        Visit(node);
        return nodes;

        void Visit(ISyntaxNode current)
        {
            nodes.Add(current);
            foreach (var child in current.Children())
                Visit(child);
        }
    }

    public static IReadOnlyList<SyntaxKind> FlattenKinds(ISyntaxNode node) =>
        [.. Flatten(node).Select(x => x.SyntaxKind)];
}
