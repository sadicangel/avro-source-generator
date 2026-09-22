using AvroSourceGenerator.Compiler;

namespace AvroSourceGenerator.Tests.Avsc;

public sealed class OptionCompositionTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void With_requires_every_value_to_be_some(bool hasLeft, bool hasRight)
    {
        var left = Create(hasLeft);
        var right = Create(hasRight);
        var expected = hasLeft && hasRight;

        Assert.Equal(expected, left.With(right).IsSome);
        Assert.Equal(expected, left.With(Option.Some(2)).With(right).IsSome);
        Assert.Equal(expected, left.With(Option.Some(2)).With(Option.Some(3)).With(right).IsSome);
        Assert.Equal(expected, left.With(Option.Some(2)).With(Option.Some(3)).With(Option.Some(4)).With(right).IsSome);
        Assert.Equal(expected, left.With(Option.Some(2)).With(Option.Some(3)).With(Option.Some(4)).With(Option.Some(5)).With(right).IsSome);
        Assert.Equal(expected, left.With(Option.Some(2)).With(Option.Some(3)).With(Option.Some(4)).With(Option.Some(5)).With(Option.Some(6)).With(right).IsSome);
    }

    [Fact]
    public void Then_does_not_invoke_callbacks_for_none()
    {
        var option = Option.None<int>();
        Assert.True(option.Then((Func<int, int>)(static _ => throw new InvalidOperationException())).IsNone);
        Assert.True(option.Then<string>(static _ => throw new InvalidOperationException()).IsNone);
        Assert.True(option.Then<string, int>(42, static (_, _) => throw new InvalidOperationException()).IsNone);
    }

    [Fact]
    public void Stateful_Then_can_extend_flat_tuples_without_captures()
    {
        var option = Option.Some(1)
            .Then(2, static (value, next) => value.With(Option.Some(next)))
            .Then(3, static (value, next) => value.With(Option.Some(next)))
            .Then(4, static (value, next) => value.With(Option.Some(next)))
            .Then(5, static (value, next) => value.With(Option.Some(next)))
            .Then(6, static (value, next) => value.With(Option.Some(next)))
            .Then(7, static (value, next) => value.With(Option.Some(next)));

        Assert.Equal((1, 2, 3, 4, 5, 6, 7), option.Value);
        Assert.True((1, 2, 3, 4, 5, 6).With(Option.None<int>()).IsNone);
    }

    [Fact]
    public void Null_converts_none_to_some_null()
    {
        Assert.Null(Option.None<string>().Null().Value);
        Assert.True(Option.Some<string?>(null).IsSome);
    }

    private static Option<int> Create(bool hasValue) => hasValue ? Option.Some(1) : Option.None<int>();
}
