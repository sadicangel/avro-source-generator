namespace AvroSourceGenerator;

public sealed class AvroAttribute(LanguageFeatures languageFeatures = LanguageFeatures.Latest) : Attribute
{
    public LanguageFeatures LanguageFeatures { get; } = languageFeatures;
}
