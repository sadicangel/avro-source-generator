using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Tests;

public sealed class CSharpNameTests
{
    [Theory]
    [InlineData("Example.Contracts")]
    [InlineData("Example")]
    [InlineData("Example..Contracts.")]
    public void Namespace_without_reserved_words_reuses_the_input(string value)
    {
        var result = CSharpName.FromSchemaName(new SchemaName("Type", value));
        Assert.Same(value, result.Namespace);
    }

    [Theory]
    [InlineData("class", "@class")]
    [InlineData("Example.class.Contracts", "Example.@class.Contracts")]
    [InlineData(".class..namespace.", ".@class..@namespace.")]
    [InlineData("class.Example.namespace", "@class.Example.@namespace")]
    [InlineData("Example..class.", "Example..@class.")]
    public void Namespace_escapes_reserved_components(string value, string expected)
    {
        Assert.Equal(expected, CSharpName.FromSchemaName(new SchemaName("Type", value)).Namespace);
    }

    [Fact]
    public void WithNullableAnnotation_IsIdempotent()
    {
        var name = new CSharpName("Type", "Namespace");

        var nullable = name.WithNullableAnnotation();

        Assert.Equal(new CSharpName("Type?", "Namespace"), nullable);
        Assert.Equal(nullable, nullable.WithNullableAnnotation());
        Assert.Equal("global::Namespace.Type?", nullable.FullName);
    }

    [Fact]
    public void WithoutNullableAnnotation_IsIdempotent()
    {
        var name = new CSharpName("Type?", "Namespace");

        var nonNullable = name.WithoutNullableAnnotation();

        Assert.Equal(new CSharpName("Type", "Namespace"), nonNullable);
        Assert.Equal(nonNullable, nonNullable.WithoutNullableAnnotation());
        Assert.Equal("global::Namespace.Type", nonNullable.FullName);
    }
}
