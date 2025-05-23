namespace AvroSourceGenerator.AvroIDL.Syntax.Names;

public abstract record class NameSyntax(SyntaxKind SyntaxKind, SyntaxTree SyntaxTree)
    : SyntaxNode(SyntaxKind, SyntaxTree)
{
    public abstract string FullName { get; }
}
