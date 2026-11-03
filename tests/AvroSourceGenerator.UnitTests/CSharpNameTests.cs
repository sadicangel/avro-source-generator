using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.UnitTests;

public sealed class CSharpNameTests
{
    [Theory]
    [InlineData("Type", null, "Member", "global::Type.Member")]
    [InlineData("Type", "Example.Contracts", "Member", "global::Example.Contracts.Type.Member")]
    [InlineData("Type", "Example", "class", "global::Example.Type.@class")]
    [InlineData("Type?", "Example", "Member", "global::Example.Type.Member")]
    [InlineData("@event?", "@namespace.@class", "namespace", "global::@namespace.@class.@event.@namespace")]
    public void Add_builds_a_qualified_member_and_escapes_its_name(string name, string? @namespace, string member, string expected)
    {
        var type = new CSharpName(name, @namespace);
        var result = type.Add(member);

        Assert.Equal(expected, result.FullName);
        Assert.Equal(result, type + member);
        Assert.Equal(name, type.Name);
        Assert.Equal(@namespace, type.Namespace);
        Assert.False(result.HasNullableAnnotation);
    }

    [Fact]
    public void Add_can_chain_members_without_repeating_the_global_prefix()
    {
        var type = new CSharpName("Outer?", "Example");
        Assert.Equal("global::Example.Outer.Nested.@class", (type + "Nested" + "class").FullName);
    }

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
