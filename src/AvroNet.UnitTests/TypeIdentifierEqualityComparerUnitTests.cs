using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AvroNet.UnitTests;

public class TypeIdentifierEqualityComparerUnitTests
{
    [Fact]
    public void Instance_IsSingleton_ReturnsSame()
    {
        var instance = TypeIdentifierEqualityComparer.Instance;

        Assert.Same(TypeIdentifierEqualityComparer.Instance, instance);
    }

    [Theory]
    [InlineData("Identifier", "Identifier")]
    [InlineData(null, null)]
    public void Equals_EqualIdentifier_ReturnsTrue(string? left, string? right)
    {
        TypeDeclarationSyntax? x = left is null ? null : ClassDeclaration(left);
        TypeDeclarationSyntax? y = right is null ? null : ClassDeclaration(right);

        bool areEqual = TypeIdentifierEqualityComparer.Instance.Equals(x, y);

        Assert.True(areEqual);
    }

    [Theory]
    [InlineData("Identifier", "identifier")]
    [InlineData(null, "Identifier")]
    [InlineData("Identifier", null)]
    public void Equals_UnequalIdentifier_ReturnsFalse(string? left, string? right)
    {
        TypeDeclarationSyntax? x = left is null ? null : ClassDeclaration(left);
        TypeDeclarationSyntax? y = right is null ? null : ClassDeclaration(right);

        bool areEqual = TypeIdentifierEqualityComparer.Instance.Equals(x, y);

        Assert.False(areEqual);
    }

    [Fact]
    public void GetHashCode_NotNull_ReturnsHashCodeOfIdentifier()
    {
        TypeDeclarationSyntax obj = ClassDeclaration("Identifier");

        int hashCode = TypeIdentifierEqualityComparer.Instance.GetHashCode(obj);

        Assert.Equal("Identifier".GetHashCode(), hashCode);
    }

    [Fact]
    public void GetHashCode_Null_ReturnsZero()
    {
        int hashCode = TypeIdentifierEqualityComparer.Instance.GetHashCode(null);

        Assert.Equal(0, hashCode);
    }
}
