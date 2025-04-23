namespace AvroSourceGenerator.Schemas;

public readonly record struct SchemaName(string Name, string? Namespace)
{
    public string FullName { get; } = Namespace is null ? Name : $"{Namespace}.{Name}";

    public SchemaName(string name) : this(name, null) { }

    public override string ToString() => FullName;
}
