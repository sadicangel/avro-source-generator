using AvroSourceGenerator.Compiler;

namespace AvroSourceGenerator.Templating;

public readonly record struct RenderOptions(GenerationTarget GenerationTarget, LanguageFeatures LanguageFeatures, AccessModifier AccessModifier)
{
    public string ObjectType => LanguageFeatures.HasNullableReferenceTypes ? "object?" : "object";
    public string FieldValueExpression => LanguageFeatures.HasNullableReferenceTypes ? "fieldValue!" : "fieldValue";
    public string Setter => LanguageFeatures.HasInitOnlyProperties ? "init" : "set";
    public string Record => LanguageFeatures.HasRecords ? "record" : "class";
    public string Fixed => GenerationTarget == GenerationTarget.Apache ? "class" : Record;
    public string Error => GenerationTarget == GenerationTarget.Apache ? "class" : Record;
}
