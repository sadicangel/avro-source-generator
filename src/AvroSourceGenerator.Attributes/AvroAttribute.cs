namespace AvroSourceGenerator;

[AttributeUsage(AttributeTargets.Class)]
public sealed class AvroAttribute : Attribute
{
    public LanguageFeatures LanguageFeatures { get; set; } = LanguageFeatures.Latest;

    public bool UseCSharpNamespace { get; set; }
}
