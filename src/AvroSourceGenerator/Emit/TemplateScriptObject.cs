using AvroSourceGenerator.Schemas;
using Scriban.Functions;
using Scriban.Runtime;

namespace AvroSourceGenerator.Emit;

internal sealed class TemplateScriptObject : BuiltinFunctions
{
    private static readonly DynamicCustomFunction s_jsonRawString = CreateFunction(static (AvroSchema schema) =>
        string.Join("\n", "\"\"\"", schema.ToJsonString(new() { Indented = true }), "\"\"\""));

    private static readonly DynamicCustomFunction s_jsonVerbatimString = CreateFunction(static (AvroSchema schema) =>
        StringFunctions.Literal(schema.ToJsonString()));

    public TemplateScriptObject(
        AvroLibrary avroLibrary,
        LanguageFeatures languageFeatures,
        string accessModifier,
        string recordDeclaration)
    {
        SetValue("json", (languageFeatures & LanguageFeatures.RawStringLiterals) != 0 ? s_jsonRawString : s_jsonVerbatimString, readOnly: true);
        SetValue("AvroLibrary", avroLibrary, readOnly: true);
        SetValue("AccessModifier", accessModifier, readOnly: true);
        SetValue("RecordDeclaration", recordDeclaration, readOnly: true);
        SetValue("UseNullableReferenceTypes", (languageFeatures & LanguageFeatures.NullableReferenceTypes) != 0, readOnly: true);
        SetValue("UseRequiredProperties", (languageFeatures & LanguageFeatures.RequiredProperties) != 0, readOnly: true);
        SetValue("UseInitOnlyProperties", (languageFeatures & LanguageFeatures.InitOnlyProperties) != 0, readOnly: true);
        SetValue("UseRawStringLiterals", (languageFeatures & LanguageFeatures.RawStringLiterals) != 0, readOnly: true);
        SetValue("UseUnsafeAccessors", (languageFeatures & LanguageFeatures.UnsafeAccessors) != 0, readOnly: true);
    }

    private static DynamicCustomFunction CreateFunction(Delegate @delegate) =>
        DynamicCustomFunction.Create(@delegate);
}
