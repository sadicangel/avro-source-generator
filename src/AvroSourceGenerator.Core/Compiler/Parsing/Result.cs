using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using AvroSourceGenerator.Diagnostics;

namespace AvroSourceGenerator.Compiler.Parsing;

[Union]
internal readonly struct Result<T> : IUnion
{
    private readonly T? _value;
    private readonly AvroDiagnostic? _error;

    public Result(T value) => (_value, _error) = (value, null);

    public Result(AvroDiagnostic diagnostic) => (_value, _error) = (default, diagnostic);

    public object? Value => IsValue ? _value : _error;

    public bool IsValue => _error is null;

    public bool IsError => !IsValue;

    public T GetValueOrDefault(T defaultValue) => IsValue ? _value! : defaultValue;

    public T? GetValueOrDefault() => IsValue ? _value! : default;

    public T GetValue() => IsValue ? _value! : throw new InvalidOperationException($"Result has no value: '{_error?.GetMessage()}'");
    public AvroDiagnostic GetError() => IsError ? _error! : throw new InvalidOperationException($"Result has no diagnostic");

    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = IsValue ? _value! : default;
        return IsValue;
    }

    public bool TryGetValue([MaybeNullWhen(false)] out AvroDiagnostic value)
    {
        value = IsError ? _error! : null;
        return IsError;
    }

    public Result<TResult> Map<TResult>(Func<T, TResult> map) => _error is null ? map(_value!) : Result.Error<TResult>(_error);

    public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> map) => _error is null ? map(_value!) : Result.Error<TResult>(_error);

    public static implicit operator Result<T>(T value) => new Result<T>(value);

    public static implicit operator Result<T>(AvroDiagnostic diagnostic) => new Result<T>(diagnostic);
}

internal static class Result
{
    public static Result<T> Value<T>(T value) => new Result<T>(value);
    public static Result<T> Error<T>(AvroDiagnostic diagnostic) => new Result<T>(diagnostic);
}
