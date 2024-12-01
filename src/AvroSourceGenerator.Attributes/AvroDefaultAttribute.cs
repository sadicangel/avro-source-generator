namespace AvroSourceGenerator;

[AttributeUsage(AttributeTargets.Enum | AttributeTargets.Property)]
public sealed class AvroDefaultAttribute(object? value) : Attribute
{
    public object? Value { get; } = value;
}
