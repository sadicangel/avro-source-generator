using AvroSourceGenerator.Extensions;

namespace AvroSourceGenerator.Schemas;

public readonly record struct MemberName(string SchemaName)
{
    public string CSharpName { get; } = SchemaName.ToValidName();

    public static implicit operator MemberName(string schemaName) => new MemberName(schemaName);

    public override string ToString() => CSharpName;
}
