using Microsoft.CodeAnalysis.CSharp;

namespace AvroSourceGenerator.Tests.Setup;

public record struct ProjectConfig(LanguageVersion LanguageVersion)
{
    private readonly Dictionary<string, string> _globalOptions = [];
    public readonly IReadOnlyDictionary<string, string> GlobalOptions => _globalOptions ?? [];

    public ProjectConfig() : this(LanguageVersion.Default) { }

    public readonly string AvroLibrary
    {
        get => _globalOptions?.GetValueOrDefault("AvroSourceGeneratorAvroLibrary") ?? string.Empty;
        set => _globalOptions?["AvroSourceGeneratorAvroLibrary"] = value;
    }

    public readonly string LanguageFeatures
    {
        get => _globalOptions?.GetValueOrDefault("AvroSourceGeneratorLanguageFeatures") ?? string.Empty;
        set => _globalOptions?["AvroSourceGeneratorLanguageFeatures"] = value;
    }

    public readonly string AccessModifier
    {
        get => _globalOptions?.GetValueOrDefault("AvroSourceGeneratorAccessModifier") ?? string.Empty;
        set => _globalOptions?["AvroSourceGeneratorAccessModifier"] = value;
    }

    public readonly string RecordDeclaration
    {
        get => _globalOptions?.GetValueOrDefault("AvroSourceGeneratorRecordDeclaration") ?? string.Empty;
        set => _globalOptions?["AvroSourceGeneratorRecordDeclaration"] = value;
    }
}
