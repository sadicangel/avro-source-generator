using AvroSourceGenerator.Extensions;

namespace AvroSourceGenerator.Schemas;

public readonly record struct MemberName(string SchemaName, string CSharpName)
{
    public MemberName(string schemaName) : this(schemaName, schemaName.ToValidName()) { }

    public static implicit operator MemberName(string schemaName) => new MemberName(schemaName);

    public override string ToString() => CSharpName;

    public bool Equals(MemberName other) => StringComparer.Ordinal.Equals(SchemaName, other.SchemaName);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(SchemaName ?? "");
}
