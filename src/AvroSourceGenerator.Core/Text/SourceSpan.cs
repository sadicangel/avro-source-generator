namespace AvroSourceGenerator.Text;

public readonly record struct SourceSpan(SourceText SourceText, int Offset, int Length) : IEquatable<SourceSpan>
{
    public static SourceSpan None => default;

    public bool IsNone => SourceText is null;

    public SourceSpan(SourceText SourceText, int Offset) : this(SourceText, Offset, SourceText.Length - Offset) { }

    public override string ToString() => IsNone ? string.Empty : SourceText.Text.AsSpan(Offset, Length).ToString();

    public bool Equals(SourceSpan other) => Equals(SourceText, other.SourceText) && Offset == other.Offset && Length == other.Length;

    public override int GetHashCode() => HashCode.Combine(SourceText, Offset, Length);
}
