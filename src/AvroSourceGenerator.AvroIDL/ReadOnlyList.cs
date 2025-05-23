using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AvroSourceGenerator.AvroIDL;

[DebuggerDisplay("Count = {Count}")]
[CollectionBuilder(typeof(ReadOnlyListBuilder), nameof(ReadOnlyListBuilder.Create))]
public sealed class ReadOnlyList<T> : IReadOnlyList<T>, IEquatable<ReadOnlyList<T>>
{
    private readonly List<T> _values;

    public ReadOnlyList() => _values = [];

    public ReadOnlyList(int capacity) => _values = new(capacity);

    public ReadOnlyList(List<T> values) => _values = values;

    public ReadOnlyList(IEnumerable<T> collection) => _values = [.. collection];

    public T this[int index] { get => _values[index]; }

    public int Count => _values.Count;

    public bool Contains(T item) => _values.Contains(item);

    public bool Equals(ReadOnlyList<T>? other) => other is not null && (ReferenceEquals(this, other) || this.SequenceEqual(other));

    public override bool Equals(object? obj) => Equals(obj as ReadOnlyList<T>);

    public override int GetHashCode() => _values.Aggregate(new HashCode(), (h, v) => { h.Add(v); return h; }, h => h.ToHashCode());

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_values).GetEnumerator();

    public int IndexOf(T item) => _values.IndexOf(item);

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_values).GetEnumerator();
}

public static class ReadOnlyListBuilder
{
    public static ReadOnlyList<T> Create<T>(ReadOnlySpan<T> values)
    {
        var list = new List<T>(values.Length);
        for (var i = 0; i < values.Length; i++)
            list.Add(values[i]);
        return new ReadOnlyList<T>([.. list]);
    }
}
