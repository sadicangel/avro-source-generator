namespace AvroSourceGenerator.AvroIDL.Syntax.Declarations;

public abstract record class DeclarationSyntax(SyntaxKind SyntaxKind, SyntaxTree SyntaxTree)
    : SyntaxNode(SyntaxKind, SyntaxTree);
