using AvroSourceGenerator.Extensions;

namespace AvroSourceGenerator.Schemas;

public readonly record struct CSharpName(string Name, string? Namespace)
{
    public string FullName { get; } = Namespace is null ? Name : $"global::{Namespace}.{Name}";

    public CSharpName(string name) : this(name, null) { }

    public override string ToString() => ToString(includeGlobalPrefix: true);

    public string ToString(bool includeGlobalPrefix) => includeGlobalPrefix || !FullName.StartsWith("global::") ? FullName : FullName[8..];

    public static CSharpName FromSchemaName(SchemaName schemaName) => new CSharpName(schemaName.Name.ToValidName(), schemaName.Namespace?.ToValidNamespace());
}
