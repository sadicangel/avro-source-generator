using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Parsing;
using Scriban.Functions;
using Scriban.Runtime;

namespace AvroSourceGenerator.Emit;

internal sealed class TemplateScriptObject : BuiltinFunctions
{
    public TemplateScriptObject(RenderSettings settings, DynamicCustomFunction? renderJsonSchema)
    {
        if (renderJsonSchema is not null)
        {
            SetValue("json", renderJsonSchema, readOnly: true);
        }

        SetValue("AvroLibrary", settings.AvroLibrary, readOnly: true);
        SetValue("AccessModifier", settings.AccessModifier, readOnly: true);
        SetValue("Record", settings.Declaration.Record, readOnly: true);
        SetValue("Error", settings.Declaration.Error, readOnly: true);
        SetValue("Fixed", settings.Declaration.Fixed, readOnly: true);
        SetValue(
            "UseNullableReferenceTypes",
            (settings.LanguageFeatures & LanguageFeatures.NullableReferenceTypes) != 0,
            readOnly: true);
        SetValue(
            "UseRequiredProperties",
            (settings.LanguageFeatures & LanguageFeatures.RequiredProperties) != 0,
            readOnly: true);
        SetValue(
            "UseInitOnlyProperties",
            (settings.LanguageFeatures & LanguageFeatures.InitOnlyProperties) != 0,
            readOnly: true);
        SetValue("UseRawStringLiterals", (settings.LanguageFeatures & LanguageFeatures.RawStringLiterals) != 0, readOnly: true);
        SetValue("UseUnsafeAccessors", (settings.LanguageFeatures & LanguageFeatures.UnsafeAccessors) != 0, readOnly: true);
    }
}
