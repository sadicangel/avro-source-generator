using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Tests;

public sealed class CSharpNameTests
{
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
