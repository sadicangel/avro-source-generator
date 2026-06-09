namespace AvroSourceGenerator.Avdl.Syntax.Annotations;

public sealed record class AnnotationNameSyntax(SeparatedSyntaxList<SyntaxToken> Identifiers) : ISyntaxNode
{
    public SyntaxKind SyntaxKind => SyntaxKind.AnnotationName;

    public string FullName => field ??= string.Join(".", Identifiers.Select(static identifier => identifier.ValueText));

    public IEnumerable<ISyntaxNode> Children() => Identifiers.SyntaxNodes;
}
