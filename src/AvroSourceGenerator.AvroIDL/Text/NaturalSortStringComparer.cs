namespace AvroSourceGenerator.AvroIDL.Text;

internal sealed class NaturalSortStringComparer(StringComparison comparison) : IComparer<string>
{
    public static NaturalSortStringComparer Ordinal { get; } = new NaturalSortStringComparer(StringComparison.Ordinal);
    public static NaturalSortStringComparer OrdinalIgnoreCase { get; } = new NaturalSortStringComparer(StringComparison.OrdinalIgnoreCase);
    public static NaturalSortStringComparer CurrentCulture { get; } = new NaturalSortStringComparer(StringComparison.CurrentCulture);
    public static NaturalSortStringComparer CurrentCultureIgnoreCase { get; } = new NaturalSortStringComparer(StringComparison.CurrentCultureIgnoreCase);
    public static NaturalSortStringComparer InvariantCulture { get; } = new NaturalSortStringComparer(StringComparison.InvariantCulture);
    public static NaturalSortStringComparer InvariantCultureIgnoreCase { get; } = new NaturalSortStringComparer(StringComparison.InvariantCultureIgnoreCase);

    public int Compare(string? x, string? y)
    {
        // Let string.Compare handle the case where x or y is null
        if (x is null || y is null)
            return string.Compare(x, y, comparison);

        var xSegments = GetSegments(x);
        var ySegments = GetSegments(y);

        while (xSegments.MoveNext() && ySegments.MoveNext())
        {
            int cmp;

            // If they're both numbers, compare the value
            if (xSegments.CurrentIsNumber && ySegments.CurrentIsNumber)
            {
                var xValue = long.Parse(xSegments.Current.ToString());
                var yValue = long.Parse(ySegments.Current.ToString());
                cmp = xValue.CompareTo(yValue);
                if (cmp != 0)
                    return cmp;
            }
            // If x is a number and y is not, x is "lesser than" y
            else if (xSegments.CurrentIsNumber)
            {
                return -1;
            }
            // If y is a number and x is not, x is "greater than" y
            else if (ySegments.CurrentIsNumber)
            {
                return 1;
            }

            // OK, neither are number, compare the segments as text
            cmp = xSegments.Current.CompareTo(ySegments.Current, comparison);
            if (cmp != 0)
                return cmp;
        }

        // At this point, either all segments are equal, or one string is shorter than the other

        // If x is shorter, it's "lesser than" y
        if (x.Length < y.Length)
            return -1;
        // If x is longer, it's "greater than" y
        if (x.Length > y.Length)
            return 1;

        // If they have the same length, they're equal
        return 0;
    }

    private static StringSegmentEnumerator GetSegments(string s) => new(s);

    private struct StringSegmentEnumerator(string s)
    {
        private int _start = -1;
        private int _length = 0;

        public readonly ReadOnlySpan<char> Current => s.AsSpan(_start, _length);

        public bool CurrentIsNumber { get; private set; } = false;

        public bool MoveNext()
        {
            var currentPosition = _start >= 0
                ? _start + _length
                : 0;

            if (currentPosition >= s.Length)
                return false;

            var start = currentPosition;
            var isFirstCharDigit = char.IsDigit(s[currentPosition]);

            while (++currentPosition < s.Length && char.IsDigit(s[currentPosition]) == isFirstCharDigit)
            {
            }

            _start = start;
            _length = currentPosition - start;
            CurrentIsNumber = isFirstCharDigit;

            return true;
        }
    }
}
