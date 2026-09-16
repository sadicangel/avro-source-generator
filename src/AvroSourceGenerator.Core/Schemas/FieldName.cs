using AvroSourceGenerator.Extensions;

namespace AvroSourceGenerator.Schemas;

public readonly record struct FieldName(string SchemaName)
{
    public string CSharpName { get; } = SchemaName.ToValidName();

    public static implicit operator FieldName(string schemaName) => new(schemaName);

    public override string ToString() => CSharpName;
}
