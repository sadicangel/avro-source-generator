
namespace AvroSourceGenerator.AvroIDL.Syntax.Types;

public sealed record class PrimitiveTypeSyntax(SyntaxTree SyntaxTree, SyntaxToken TypeKeyword)
    : TypeSyntax(GetKindFromKeyword(TypeKeyword.SyntaxKind), SyntaxTree)
{
    public override IEnumerable<SyntaxNode> Children()
    {
        yield return TypeKeyword;
    }

    private static SyntaxKind GetKindFromKeyword(SyntaxKind syntaxKind) => syntaxKind switch
    {
        SyntaxKind.VoidKeyword => SyntaxKind.VoidType,
        SyntaxKind.NullKeyword => SyntaxKind.NullType,
        SyntaxKind.IntKeyword => SyntaxKind.IntType,
        SyntaxKind.LongKeyword => SyntaxKind.LongType,
        SyntaxKind.StringKeyword => SyntaxKind.StringType,
        SyntaxKind.BooleanKeyword => SyntaxKind.BooleanType,
        SyntaxKind.FloatKeyword => SyntaxKind.FloatType,
        SyntaxKind.DoubleKeyword => SyntaxKind.DoubleType,
        SyntaxKind.BytesKeyword => SyntaxKind.BytesType,

        _ => throw new ArgumentOutOfRangeException(nameof(syntaxKind), syntaxKind, null)
    };
}

