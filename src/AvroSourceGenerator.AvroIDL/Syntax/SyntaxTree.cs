using AvroSourceGenerator.AvroIDL.Diagnostics;
using AvroSourceGenerator.AvroIDL.Parsing;
using AvroSourceGenerator.AvroIDL.Scanning;
using AvroSourceGenerator.AvroIDL.Text;

namespace AvroSourceGenerator.AvroIDL.Syntax;
public sealed class SyntaxTree
{
    public SourceText SourceText { get; }

    public CompilationUnitSyntax CompilationUnit => field ??= Parser.Parse(this);

    public DiagnosticBag Diagnostics { get; } = [];

    private Dictionary<SyntaxNode, SyntaxNode?> NodeParents => field ??= CreateNodeParents(CompilationUnit);

    private SyntaxTree(SourceText sourceText)
    {
        SourceText = sourceText;
    }

    internal SyntaxNode? GetParent(SyntaxNode node) => NodeParents[node];

    public static SyntaxList<SyntaxToken> Scan(SourceText sourceText) => [.. Scanner.Scan(new SyntaxTree(sourceText))];

    public static SyntaxTree Parse(SourceText sourceText) => new(sourceText);


    private static Dictionary<SyntaxNode, SyntaxNode?> CreateNodeParents(CompilationUnitSyntax root)
    {
        var result = new Dictionary<SyntaxNode, SyntaxNode?> { [root] = null };
        CreateParentsDictionary(result, root);
        return result;

        static void CreateParentsDictionary(Dictionary<SyntaxNode, SyntaxNode?> result, SyntaxNode node)
        {
            foreach (var child in node.Children())
            {
                result.Add(child, node);
                CreateParentsDictionary(result, child);
            }
        }
    }
}
