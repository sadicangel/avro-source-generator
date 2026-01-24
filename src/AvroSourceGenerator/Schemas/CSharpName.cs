using AvroSourceGenerator.Registry.Extensions;

namespace AvroSourceGenerator.Schemas;

public readonly record struct CSharpName(string Name, string? Namespace)
{
    public string FullName { get; } = Namespace is null ? Name : $"global::{Namespace}.{Name}";

    public CSharpName(string name) : this(name, null) { }

    public override string ToString() => FullName;

    public static CSharpName FromSchemaName(SchemaName schemaName) =>
        new(schemaName.Name.ToValidName(), schemaName.Namespace?.GetValidNamespace());
}
