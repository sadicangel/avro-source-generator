using AvroSourceGenerator.Compiler;

namespace AvroSourceGenerator.Templating;

public readonly record struct RenderOptions(GenerationTarget GenerationTarget, LanguageFeatures LanguageFeatures, AccessModifier AccessModifier)
{
    public bool UseNullableReferenceTypes => (LanguageFeatures & LanguageFeatures.NullableReferenceTypes) != 0;
    public bool UseRecords => (LanguageFeatures & LanguageFeatures.Records) != 0;
    public bool UseInitOnlyProperties => (LanguageFeatures & LanguageFeatures.InitOnlyProperties) != 0;
    public bool UseRequiredProperties => (LanguageFeatures & LanguageFeatures.RequiredProperties) != 0;
    public bool UseRawStringLiterals => (LanguageFeatures & LanguageFeatures.RawStringLiterals) != 0;
    public bool UseUnsafeAccessors => (LanguageFeatures & LanguageFeatures.UnsafeAccessors) != 0;

    public string ObjectType => UseNullableReferenceTypes ? "object?" : "object";
    public string FieldValueExpression => UseNullableReferenceTypes ? "fieldValue!" : "fieldValue";
    public string Setter => UseInitOnlyProperties ? "init" : "set";
    public string Record => UseRecords ? "record" : "class";
    public string Fixed => GenerationTarget == GenerationTarget.Apache ? "class" : Record;
    public string Error => GenerationTarget == GenerationTarget.Apache ? "class" : Record;
}
