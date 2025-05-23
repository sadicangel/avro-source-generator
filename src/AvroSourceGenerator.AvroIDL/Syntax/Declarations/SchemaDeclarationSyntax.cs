namespace AvroSourceGenerator.AvroIDL.Syntax.Declarations;

public abstract record class SchemaDeclarationSyntax(SyntaxKind SyntaxKind, SyntaxTree SyntaxTree)
    : DeclarationSyntax(SyntaxKind, SyntaxTree);
