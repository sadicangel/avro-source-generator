namespace AvroSourceGenerator.Schemas;

public readonly record struct SchemaName(string Name, string? Namespace)
{
    public string FullName { get; } = Namespace is null ? Name : $"{Namespace}.{Name}";

    public SchemaName(string name) : this(name, null) { }

    public override string ToString() => FullName;

    public SchemaName ResolveIn(string? containingNamespace) =>
        Namespace is null && containingNamespace is not null ? new SchemaName(Name, containingNamespace) : this;

    public string RelativeTo(string? containingNamespace) =>
        Namespace == containingNamespace ? Name : FullName;
}
