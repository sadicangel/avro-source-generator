namespace AvroSourceGenerator.AvroIDL.Syntax.Names;

public record class QualifiedNameSyntax(
    SyntaxTree SyntaxTree,
    NameSyntax Left,
    SyntaxToken DotToken,
    SimpleNameSyntax Right)
    : NameSyntax(SyntaxKind.QualifiedName, SyntaxTree)
{
    public override string FullName => field ??= string.Join(SyntaxFacts.GetText(SyntaxKind.DotToken), EnumerateNames(this));

    private static IEnumerable<string> EnumerateNames(NameSyntax name)
    {
        if (name is SimpleNameSyntax simpleName)
        {
            yield return simpleName.FullName;
        }
        else
        {
            var qualifiedName = (QualifiedNameSyntax)name;
            foreach (var left in EnumerateNames(qualifiedName.Left))
                yield return left;

            yield return qualifiedName.Right.FullName;
        }
    }

    public override IEnumerable<SyntaxNode> Children()
    {
        yield return Left;
        yield return DotToken;
        yield return Right;
    }
}
