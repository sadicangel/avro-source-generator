namespace AvroSourceGenerator;

[AttributeUsage(AttributeTargets.Class)]
public sealed class AvroAttribute : Attribute
{
    public AvroLibrary AvroLibrary { get; set; }
    public LanguageFeatures LanguageFeatures { get; set; }
}
