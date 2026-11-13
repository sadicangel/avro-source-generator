using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace AvroSourceGenerator.Compiler.Parsing;

[Union]
internal readonly struct Option<T> : IUnion
{
    private readonly T? _value;

    public static Option<T> None => new Option<T>(new None());

    public Option(T value) => (_value, IsSome) = (value, true);

    public Option(None _) : this() { }

    public object? Value => IsSome ? _value : new None();

    public bool IsSome { get; }

    public bool IsNone => !IsSome;

    public T GetValueOrDefault(T defaultValue) => IsSome ? _value! : defaultValue;

    public T? GetValueOrDefault() => IsSome ? _value! : default;

    public T GetValue() => IsSome ? _value! : throw new InvalidOperationException("Option has no value.");

    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = IsSome ? _value! : default;
        return IsSome;
    }

    public bool TryGetValue(out None value)
    {
        value = default;
        return !IsSome;
    }

    public Option<TResult> Map<TResult>(Func<T, TResult> map) => IsSome ? map(_value!) : Option.None<TResult>();

    public Option<TResult> Bind<TResult>(Func<T, Option<TResult>> map) => IsSome ? map(_value!) : Option.None<TResult>();

    public Result<Option<TResult>> Traverse<TResult>(Func<T, Result<TResult>> map) => IsNone ? Result.Value(Option.None<TResult>()) : map(_value!).Map(Option.Some);

    public Result<Option<TResult>> TraverseBind<TResult>(Func<T, Result<Option<TResult>>> bind) => IsSome ? bind(_value!) : Result.Value<Option<TResult>>(new None());

    public static implicit operator Option<T>(T value) => new Option<T>(value);

    public static implicit operator Option<T>(None _) => None;
}

internal readonly struct None;

internal static class Option
{
    public static Option<T> Some<T>(T value) => new Option<T>(value);
    public static Option<T> None<T>() => new Option<T>(new None());
}
