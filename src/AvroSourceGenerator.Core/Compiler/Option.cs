using System.Diagnostics.CodeAnalysis;

namespace AvroSourceGenerator.Compiler;

public static class Option
{
    public static Option<T> None<T>() => Option<T>.CreateNone();
    public static Option<T> Some<T>(T value) => Option<T>.CreateSome(value);

    public static Option<(T1, T2)> With<T1, T2>(this T1 arg0, Option<T2> arg1) =>
        arg1.IsSome ? Some((arg0, arg1.Value)) : None<(T1, T2)>();

    public static Option<(T1, T2)> With<T1, T2>(this Option<T1> arg0, T2 arg1) =>
        arg0.IsSome ? Some((arg0.Value, arg1)) : None<(T1, T2)>();

    public static Option<(T1, T2)> With<T1, T2>(this Option<T1> arg0, Option<T2> arg1) =>
        arg0.IsSome && arg1.IsSome ? Some((arg0.Value, arg1.Value)) : None<(T1, T2)>();

    public static Option<(T1, T2, T3)> With<T1, T2, T3>(this Option<(T1, T2)> arg0, Option<T3> arg1) =>
        arg0.IsSome && arg1.IsSome ? Some((arg0.Value.Item1, arg0.Value.Item2, arg1.Value)) : None<(T1, T2, T3)>();

    public static Option<(T1, T2, T3, T4)> With<T1, T2, T3, T4>(this Option<(T1, T2, T3)> arg0, Option<T4> arg1) =>
        arg0.IsSome && arg1.IsSome ? Some((arg0.Value.Item1, arg0.Value.Item2, arg0.Value.Item3, arg1.Value)) : None<(T1, T2, T3, T4)>();

    public static Option<(T1, T2, T3, T4, T5)> With<T1, T2, T3, T4, T5>(this Option<(T1, T2, T3, T4)> arg0, Option<T5> arg1) =>
        arg0.IsSome && arg1.IsSome ? Some((arg0.Value.Item1, arg0.Value.Item2, arg0.Value.Item3, arg0.Value.Item4, arg1.Value)) : None<(T1, T2, T3, T4, T5)>();

    public static Option<(T1, T2, T3, T4, T5, T6)> With<T1, T2, T3, T4, T5, T6>(this Option<(T1, T2, T3, T4, T5)> arg0, Option<T6> arg1) =>
        arg0.IsSome && arg1.IsSome ? Some((arg0.Value.Item1, arg0.Value.Item2, arg0.Value.Item3, arg0.Value.Item4, arg0.Value.Item5, arg1.Value)) : None<(T1, T2, T3, T4, T5, T6)>();

    public static Option<(T1, T2, T3, T4, T5, T6, T7)> With<T1, T2, T3, T4, T5, T6, T7>(this Option<(T1, T2, T3, T4, T5, T6)> arg0, Option<T7> arg1) =>
        arg0.IsSome && arg1.IsSome ? Some((arg0.Value.Item1, arg0.Value.Item2, arg0.Value.Item3, arg0.Value.Item4, arg0.Value.Item5, arg0.Value.Item6, arg1.Value)) : None<(T1, T2, T3, T4, T5, T6, T7)>();

    public static Option<(T1, T2, T3, T4, T5, T6, T7, T8)> With<T1, T2, T3, T4, T5, T6, T7, T8>(this Option<(T1, T2, T3, T4, T5, T6, T7)> arg0, Option<T8> arg1) =>
        arg0.IsSome && arg1.IsSome ? Some((arg0.Value.Item1, arg0.Value.Item2, arg0.Value.Item3, arg0.Value.Item4, arg0.Value.Item5, arg0.Value.Item6, arg0.Value.Item7, arg1.Value)) : None<(T1, T2, T3, T4, T5, T6, T7, T8)>();

    public static Option<(T1, T2, T3)> With<T1, T2, T3>(this (T1, T2) arg0, Option<T3> arg1) =>
        arg1.Then(arg0, static (value, state) => (state.Item1, state.Item2, value));

    public static Option<(T1, T2, T3, T4)> With<T1, T2, T3, T4>(this (T1, T2, T3) arg0, Option<T4> arg1) =>
        arg1.Then(arg0, static (value, state) => (state.Item1, state.Item2, state.Item3, value));

    public static Option<(T1, T2, T3, T4, T5)> With<T1, T2, T3, T4, T5>(this (T1, T2, T3, T4) arg0, Option<T5> arg1) =>
        arg1.Then(arg0, static (value, state) => (state.Item1, state.Item2, state.Item3, state.Item4, value));

    public static Option<(T1, T2, T3, T4, T5, T6)> With<T1, T2, T3, T4, T5, T6>(this (T1, T2, T3, T4, T5) arg0, Option<T6> arg1) =>
        arg1.Then(arg0, static (value, state) => (state.Item1, state.Item2, state.Item3, state.Item4, state.Item5, value));

    public static Option<(T1, T2, T3, T4, T5, T6, T7)> With<T1, T2, T3, T4, T5, T6, T7>(this (T1, T2, T3, T4, T5, T6) arg0, Option<T7> arg1) =>
        arg1.Then(arg0, static (value, state) => (state.Item1, state.Item2, state.Item3, state.Item4, state.Item5, state.Item6, value));
}

public readonly struct Option<TValue>
{
    private readonly TValue? _value;

    private Option(TValue? value, bool isSome)
    {
        _value = value;
        IsSome = isSome;
    }

    public bool IsSome { get; }
    public bool IsNone => !IsSome;

    public TValue Value => IsSome ? _value! : throw new InvalidOperationException("The option has no value.");

    public void Deconstruct(out TValue value, out bool isSome)
    {
        value = _value!;
        isSome = IsSome;
    }

    public bool TryGetValue([MaybeNullWhen(false)] out TValue value)
    {
        value = _value!;
        return IsSome;
    }

    public Option<TOther> Then<TOther>(Func<TValue, Option<TOther>> then) =>
        IsSome ? then(Value) : Option.None<TOther>();

    public Option<TOther> Then<TOther, TState>(TState state, Func<TValue, TState, Option<TOther>> then) =>
        IsSome ? then(Value, state) : Option.None<TOther>();

    public Option<TOther> Then<TOther>(Func<TValue, TOther> then) =>
        IsSome ? then(Value) : Option.None<TOther>();

    public Option<TOther> Then<TOther, TState>(TState state, Func<TValue, TState, TOther> then) =>
        IsSome ? then(Value, state) : Option.None<TOther>();

    public Option<TValue?> Null() => Option.Some(IsSome ? Value : default);

    public static implicit operator Option<TValue>(TValue value) => Option.Some(value);

    public static Option<TValue> CreateNone() => default;

    public static Option<TValue> CreateSome(TValue value) => new(value, isSome: true);
}
