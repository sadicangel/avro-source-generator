namespace AvroSourceGenerator.Text;

public readonly record struct SourceSpan
{
    public SourceSpan(SourceText SourceText, int Offset, int Length)
    {
        ArgumentNullException.ThrowIfNull(SourceText);
        if (Offset < 0 || Offset > SourceText.Length)
            throw new ArgumentOutOfRangeException(nameof(Offset));
        if (Length < 0 || Length > SourceText.Length - Offset)
            throw new ArgumentOutOfRangeException(nameof(Length));
        this.SourceText = SourceText;
        this.Offset = Offset;
        this.Length = Length;
    }

    public SourceText SourceText { get; }
    public int Offset { get; }
    public int Length { get; }

    public static SourceSpan None => default;

    public bool IsNone => SourceText is null;

    public bool IsNoneOrEmpty => IsNone || SourceText.IsEmpty;

    public SourceSpan(SourceText SourceText, int Offset) : this(SourceText, Offset, (SourceText ?? throw new ArgumentNullException(nameof(SourceText))).Length - Offset) { }

    public override string ToString() => AsSpan().ToString();

    public ReadOnlySpan<char> AsSpan() => SourceText is not null ? SourceText.Text.AsSpan(Offset, Length) : ReadOnlySpan<char>.Empty;

    public bool Equals(SourceSpan other) => Equals(SourceText, other.SourceText) && Offset == other.Offset && Length == other.Length;

    public override int GetHashCode() => HashCode.Combine(SourceText, Offset, Length);
}
