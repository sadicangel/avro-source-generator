namespace AvroSourceGenerator;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Property)]
public sealed class AvroAliasesAttribute(params string[] aliases) : Attribute
{
    public string[] Aliases { get; } = aliases;
}
