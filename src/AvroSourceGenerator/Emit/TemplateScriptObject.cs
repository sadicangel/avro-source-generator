using System.Text.Json;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Parsing;
using AvroSourceGenerator.Schemas;
using Scriban.Functions;
using Scriban.Runtime;

namespace AvroSourceGenerator.Emit;

internal sealed class TemplateScriptObject : BuiltinFunctions
{
    private static readonly DynamicCustomFunction s_jsonRawString = CreateFunction(static (AvroSchema schema) =>
        string.Join("\n", "\"\"\"", schema.ToJsonString(new JsonWriterOptions { Indented = true }), "\"\"\""));

    private static readonly DynamicCustomFunction s_jsonVerbatimString = CreateFunction(static (AvroSchema schema) =>
        StringFunctions.Literal(schema.ToJsonString()));

    public TemplateScriptObject(RenderSettings settings)
    {
        SetValue(
            "json",
            (settings.LanguageFeatures & LanguageFeatures.RawStringLiterals) != 0 ? s_jsonRawString : s_jsonVerbatimString,
            readOnly: true);
        SetValue("AvroLibrary", settings.AvroLibrary, readOnly: true);
        SetValue("AccessModifier", settings.AccessModifier, readOnly: true);
        SetValue("RecordDeclaration", settings.RecordDeclaration, readOnly: true);
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

    private static DynamicCustomFunction CreateFunction(Delegate @delegate) =>
        DynamicCustomFunction.Create(@delegate);
}
