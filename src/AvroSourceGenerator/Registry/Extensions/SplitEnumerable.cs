namespace AvroSourceGenerator.Registry.Extensions;

internal readonly ref struct SplitEnumerable(ReadOnlySpan<char> value, char separator)
{
    private readonly ReadOnlySpan<char> _value = value;

    public SplitEnumerator GetEnumerator() => new SplitEnumerator(_value, separator);

    public ref struct SplitEnumerator(ReadOnlySpan<char> value, char separator)
    {
        private readonly ReadOnlySpan<char> _value = value;
        private int _start = -1;
        public ReadOnlySpan<char> Current { get; private set; } = default;

        public bool MoveNext()
        {
            if (_start >= _value.Length)
                return false;

            var end = _value[(_start + 1)..].IndexOf(separator);
            if (end == -1)
            {
                Current = _value[(_start + 1)..];
                _start = _value.Length;
                return true;
            }

            Current = _value.Slice(_start + 1, end);
            _start += end + 1;

            return true;
        }
    }
}
