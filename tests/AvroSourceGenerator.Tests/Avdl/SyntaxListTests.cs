using AvroSourceGenerator.Avdl.Syntax;

namespace AvroSourceGenerator.Tests.Avdl;

public sealed class SyntaxListTests
{
    [Fact]
    public void Default_syntax_list_behaves_as_empty()
    {
        var list = default(SyntaxList<SyntaxToken>);

        Assert.Empty(list);
        Assert.False(list.SyntaxNodes.IsDefault);
        Assert.Equal(new SyntaxList<SyntaxToken>([]), list);
        Assert.Equal(new SyntaxList<SyntaxToken>([]).GetHashCode(), list.GetHashCode());
    }

    [Fact]
    public void Default_separated_syntax_list_behaves_as_empty()
    {
        var list = default(SeparatedSyntaxList<SyntaxToken>);

        Assert.Empty(list);
        Assert.False(list.SyntaxNodes.IsDefault);
        Assert.Equal(new SeparatedSyntaxList<SyntaxToken>([]), list);
        Assert.Equal(new SeparatedSyntaxList<SyntaxToken>([]).GetHashCode(), list.GetHashCode());
    }
}
