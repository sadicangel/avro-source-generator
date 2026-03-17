using AvroSourceGenerator.Configuration;

namespace AvroSourceGenerator.Templating;

public readonly record struct TemplateSettings(TargetProfile TargetProfile, LanguageFeatures LanguageFeatures, AccessModifier AccessModifier)
{
    public bool UseNullableReferenceTypes => (LanguageFeatures & LanguageFeatures.NullableReferenceTypes) != 0;
    public bool UseRecords => (LanguageFeatures & LanguageFeatures.Records) != 0;
    public bool UseInitOnlyProperties => (LanguageFeatures & LanguageFeatures.InitOnlyProperties) != 0;
    public bool UseRequiredProperties => (LanguageFeatures & LanguageFeatures.RequiredProperties) != 0;
    public bool UseRawStringLiterals => (LanguageFeatures & LanguageFeatures.RawStringLiterals) != 0;
    public bool UseUnsafeAccessors => (LanguageFeatures & LanguageFeatures.UnsafeAccessors) != 0;

    public string Record => UseRecords ? "record" : "class";
    public string Fixed => TargetProfile == TargetProfile.Apache ? "class" : Record;
    public string Error => TargetProfile == TargetProfile.Apache ? "class" : Record;
}
