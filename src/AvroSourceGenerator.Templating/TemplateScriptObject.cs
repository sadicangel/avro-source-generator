using AvroSourceGenerator.Compiler;
using Scriban.Functions;

namespace AvroSourceGenerator.Templating;

internal sealed class TemplateScriptObject : BuiltinFunctions
{
    private static readonly ApacheFunctions s_apacheFunctions = new ApacheFunctions();

    public TemplateScriptObject(RenderOptions options)
    {
        SetValue("GenerationTarget", options.GenerationTarget, readOnly: true);
        SetValue("AccessModifier", options.AccessModifier.Keyword, readOnly: true);
        SetValue("Record", options.Record, readOnly: true);
        SetValue("Error", options.Error, readOnly: true);
        SetValue("Fixed", options.Fixed, readOnly: true);
        SetValue("ObjectType", options.ObjectType, readOnly: true);
        SetValue("FieldValueExpression", options.FieldValueExpression, readOnly: true);
        SetValue("Setter", options.Setter, readOnly: true);
        SetValue("UseNullableReferenceTypes", options.LanguageFeatures.HasNullableReferenceTypes, readOnly: true);
        SetValue("UseRequiredProperties", options.LanguageFeatures.HasRequiredProperties, readOnly: true);
        SetValue("UseInitOnlyProperties", options.LanguageFeatures.HasInitOnlyProperties, readOnly: true);
        SetValue("UseRawStringLiterals", options.LanguageFeatures.HasRawStringLiterals, readOnly: true);
        SetValue("UseUnsafeAccessors", options.LanguageFeatures.HasUnsafeAccessors, readOnly: true);
        SetValue("UseUnions", options.LanguageFeatures.HasUnions, readOnly: true);
        SetValue("apache", s_apacheFunctions, readOnly: true);
    }
}
