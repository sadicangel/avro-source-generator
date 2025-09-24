using System.Collections.Immutable;

namespace AvroSourceGenerator.Registry.Extensions;

internal readonly ref struct SplitEnumerable(ReadOnlySpan<char> value, char separator)
{
    private readonly ReadOnlySpan<char> _value = value;
    private readonly char _separator = separator;

    public SplitEnumerator GetEnumerator() => new(_value, _separator);

    public ref struct SplitEnumerator
    {
        private readonly ReadOnlySpan<char> _value;
        private readonly char _separator;
        private int _start;
        public SplitEnumerator(ReadOnlySpan<char> value, char separator)
        {
            _value = value;
            _separator = separator;
            _start = -1;
            Current = default;
        }
        public ReadOnlySpan<char> Current { get; private set; }

        public bool MoveNext()
        {
            if (_start >= _value.Length)
                return false;

            var end = _value[(_start + 1)..].IndexOf(_separator);
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
