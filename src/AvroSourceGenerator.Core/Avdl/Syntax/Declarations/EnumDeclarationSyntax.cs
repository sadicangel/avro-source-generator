namespace AvroSourceGenerator.Avdl.Syntax.Declarations;

public sealed record class EnumDeclarationSyntax(
    SyntaxToken EnumKeyword,
    SimpleNameSyntax Name,
    SyntaxList<DocumentationSyntax> Documentation,
    SyntaxList<AnnotationSyntax> Annotations,
    SyntaxToken BraceOpenToken,
    SeparatedSyntaxList<SimpleNameSyntax> Symbols,
    SyntaxToken BraceCloseToken,
    DefaultValueClauseSyntax? DefaultValue,
    SyntaxToken? SemicolonToken)
    : ISchemaDeclarationSyntax
{
    public SyntaxKind SyntaxKind => SyntaxKind.EnumDeclaration;

    public IEnumerable<ISyntaxNode> Children()
    {
        yield return EnumKeyword;
        yield return Name;
        foreach (var documentation in Documentation)
            yield return documentation;
        foreach (var annotation in Annotations)
            yield return annotation;
        yield return BraceOpenToken;
        foreach (var symbol in Symbols)
            yield return symbol;
        yield return BraceCloseToken;
        if (DefaultValue is not null)
            yield return DefaultValue;
        if (SemicolonToken is not null)
            yield return SemicolonToken;
    }
}
