using Scriban.Functions;

namespace AvroSourceGenerator.Templating;

internal sealed class TemplateScriptObject : BuiltinFunctions
{
    public TemplateScriptObject(RenderOptions options)
    {
        SetValue("TargetProfile", options.TargetProfile, readOnly: true);
        SetValue("AccessModifier", options.AccessModifier.Keyword, readOnly: true);
        SetValue("Record", options.Record, readOnly: true);
        SetValue("Error", options.Error, readOnly: true);
        SetValue("Fixed", options.Fixed, readOnly: true);
        SetValue("ObjectType", options.ObjectType, readOnly: true);
        SetValue("FieldValueExpression", options.FieldValueExpression, readOnly: true);
        SetValue("Setter", options.Setter, readOnly: true);
        SetValue("UseNullableReferenceTypes", options.UseNullableReferenceTypes, readOnly: true);
        SetValue("UseRequiredProperties", options.UseRequiredProperties, readOnly: true);
        SetValue("UseInitOnlyProperties", options.UseInitOnlyProperties, readOnly: true);
        SetValue("UseRawStringLiterals", options.UseRawStringLiterals, readOnly: true);
        SetValue("UseUnsafeAccessors", options.UseUnsafeAccessors, readOnly: true);
    }
}
