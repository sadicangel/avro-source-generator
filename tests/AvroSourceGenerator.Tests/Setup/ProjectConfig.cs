using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.Tests.Setup;

public record struct ProjectConfig(LanguageVersion LanguageVersion)
{
    public Dictionary<string, string> GlobalOptions => field ??= [];

    public string AvroLibrary
    {
        get => GlobalOptions.GetValueOrDefault("AvroSourceGeneratorAvroLibrary") ?? string.Empty;
        set => GlobalOptions["AvroSourceGeneratorAvroLibrary"] = value;
    }

    public string LanguageFeatures
    {
        get => GlobalOptions.GetValueOrDefault("AvroSourceGeneratorLanguageFeatures") ?? string.Empty;
        set => GlobalOptions["AvroSourceGeneratorLanguageFeatures"] = value;
    }

    public string AccessModifier
    {
        get => GlobalOptions.GetValueOrDefault("AvroSourceGeneratorAccessModifier") ?? string.Empty;
        set => GlobalOptions["AvroSourceGeneratorAccessModifier"] = value;
    }

    public string RecordDeclaration
    {
        get => GlobalOptions.GetValueOrDefault("AvroSourceGeneratorRecordDeclaration") ?? string.Empty;
        set => GlobalOptions["AvroSourceGeneratorRecordDeclaration"] = value;
    }
}
