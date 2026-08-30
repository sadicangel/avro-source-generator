using Scriban.Functions;

namespace AvroSourceGenerator.Templating;

internal sealed class TemplateScriptObject : BuiltinFunctions
{
    public TemplateScriptObject(TemplateSettings settings)
    {
        SetValue("TargetProfile", settings.TargetProfile, readOnly: true);
        SetValue("AccessModifier", settings.AccessModifier.Keyword, readOnly: true);
        SetValue("Record", settings.Record, readOnly: true);
        SetValue("Error", settings.Error, readOnly: true);
        SetValue("Fixed", settings.Fixed, readOnly: true);
        SetValue("ObjectType", settings.ObjectType, readOnly: true);
        SetValue("FieldValueExpression", settings.FieldValueExpression, readOnly: true);
        SetValue("Setter", settings.Setter, readOnly: true);
        SetValue("UseNullableReferenceTypes", settings.UseNullableReferenceTypes, readOnly: true);
        SetValue("UseRequiredProperties", settings.UseRequiredProperties, readOnly: true);
        SetValue("UseInitOnlyProperties", settings.UseInitOnlyProperties, readOnly: true);
        SetValue("UseRawStringLiterals", settings.UseRawStringLiterals, readOnly: true);
        SetValue("UseUnsafeAccessors", settings.UseUnsafeAccessors, readOnly: true);
    }
}
