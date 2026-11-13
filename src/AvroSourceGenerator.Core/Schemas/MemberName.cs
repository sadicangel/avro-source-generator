using AvroSourceGenerator.Extensions;

namespace AvroSourceGenerator.Schemas;

public readonly record struct MemberName(string SchemaName)
{
    public string CSharpName { get; } = SchemaName.ToValidName();

    public static implicit operator MemberName(string schemaName) => new MemberName(schemaName);

    public override string ToString() => CSharpName;

    public bool Equals(MemberName other) => StringComparer.Ordinal.Equals(SchemaName, other.SchemaName);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(SchemaName ?? "");
}
