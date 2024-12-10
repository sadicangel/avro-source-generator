using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace AvroSourceGenerator;

internal sealed record class SourceOutputModel(
    LanguageFeatures LanguageFeatures,
    string ContainingNamespace,
    string? NamespaceOverride,
    string RecordDeclaration,
    string AccessModifier,
    SchemaFieldInfo SchemaField,
    EquatableArray<Diagnostic> Diagnostics);

internal readonly record struct SchemaFieldInfo(string SchemaJson, Location Location)
{
    public bool IsValid => SchemaJson is not null && Location is not null;
}

[CollectionBuilder(typeof(EquatableArrayBuilder), nameof(EquatableArrayBuilder.Create))]
internal readonly struct EquatableArray<T>(ImmutableArray<T> array) : IEquatable<EquatableArray<T>>
    where T : IEquatable<T>
{
    public static readonly EquatableArray<T> Empty = new([]);

    private readonly ImmutableArray<T> _array = array;

    public int Length => _array.Length;

    public bool Equals(EquatableArray<T> other) => _array.SequenceEqual(other._array);

    public override bool Equals(object? obj) => obj is EquatableArray<T> array && Equals(array);

    public override int GetHashCode() =>
        _array.Aggregate(new HashCode(), (h, c) => { h.Add(c); return h; }, h => h.ToHashCode());
    public ImmutableArray<T>.Enumerator GetEnumerator() => _array.GetEnumerator();

    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);

}

internal static class EquatableArrayBuilder
{
    public static EquatableArray<T> Create<T>(ReadOnlySpan<T> values) where T : IEquatable<T> =>
        new(ImmutableArray.Create(values));
}
