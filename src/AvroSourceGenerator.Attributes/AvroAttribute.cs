namespace AvroSourceGenerator;

[AttributeUsage(AttributeTargets.Class)]
public sealed class AvroAttribute(string avroSchema) : Attribute
{
    public string AvroSchema { get; } = avroSchema;

    public LanguageFeatures LanguageFeatures { get; set; }

    public bool UseCSharpNamespace { get; set; }
}
