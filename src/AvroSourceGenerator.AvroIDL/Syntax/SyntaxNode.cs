using AvroSourceGenerator.AvroIDL.Text;

namespace AvroSourceGenerator.AvroIDL.Syntax;
public abstract record class SyntaxNode(SyntaxKind SyntaxKind, SyntaxTree SyntaxTree)
{
    public SyntaxNode? Parent => SyntaxTree.GetParent(this);

    public SyntaxToken FirstToken => this is SyntaxToken token ? token : Children().First().FirstToken;
    public SyntaxToken LastToken => this is SyntaxToken token ? token : Children().Last().LastToken;

    public virtual SourceSpan SourceSpan => SourceSpan.Combine(Children().First().SourceSpan, Children().Last().SourceSpan);
    public virtual SourceSpan SourceSpanWithTrivia => SourceSpan.Combine(Children().First().SourceSpanWithTrivia, Children().Last().SourceSpanWithTrivia);

    public sealed override string ToString() =>
        SourceSpan.Length > 0 ? $"{SyntaxKind} {SourceSpan}" : $"{SyntaxKind}";

    public abstract IEnumerable<SyntaxNode> Children();

    public IEnumerable<SyntaxNode> SelfAndDescendants()
    {
        yield return this;

        var queue = new Queue<SyntaxNode>();
        queue.Enqueue(this);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            foreach (var child in node.Children())
            {
                yield return child;
                queue.Enqueue(child);
            }
        }
    }

    public IEnumerable<SyntaxNode> Descendants() => SelfAndDescendants().Skip(1);

    public IEnumerable<SyntaxNode> SelfAndAncestors()
    {
        var node = this;
        while (node is not null)
        {
            yield return node;
            node = node.Parent;
        }
    }

    public IEnumerable<SyntaxNode> Ancestors() => SelfAndAncestors().Skip(1);
}
