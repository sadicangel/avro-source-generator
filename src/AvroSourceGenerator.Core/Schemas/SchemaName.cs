using AvroSourceGenerator.Extensions;

namespace AvroSourceGenerator.Schemas;

public readonly record struct SchemaName(string Name, string? Namespace)
{
    public bool IsDefault => Name is null;

    public bool IsQualified => Namespace is not null;

    public string FullName { get; } = Namespace is null ? Name : $"{Namespace}.{Name}";

    public SchemaName(string name) : this(name, null) { }

    internal static SchemaName FromFullName(string fullName)
    {
        fullName.TrySplitQualifiedName(out var name, out var ns);
        return new SchemaName(name, ns);
    }

    public override string ToString() => FullName;

    public SchemaName ResolveIn(string? containingNamespace) =>
        Namespace is null && containingNamespace is not null ? new SchemaName(Name, containingNamespace) : this;
}
