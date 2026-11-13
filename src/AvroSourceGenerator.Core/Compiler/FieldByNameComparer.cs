using AvroSourceGenerator.Schemas;

namespace AvroSourceGenerator.Compiler;

internal sealed class FieldByNameComparer : IEqualityComparer<Field?>
{
    public static FieldByNameComparer Instance { get; } = new FieldByNameComparer();

    public bool Equals(Field? x, Field? y) => ReferenceEquals(x, y) || (x is not null && y is not null && x.Name == y.Name);
    public int GetHashCode(Field? obj) => obj?.Name.GetHashCode() ?? 0;
}
