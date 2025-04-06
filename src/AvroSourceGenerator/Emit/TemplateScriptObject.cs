using System.Text.Json;
using Scriban.Functions;
using Scriban.Runtime;

namespace AvroSourceGenerator.Emit;

internal sealed class TemplateScriptObject : BuiltinFunctions
{
    private static readonly JsonSerializerOptions s_jsonOptionsRawStringLiteral =
        new() { WriteIndented = true };
    private static readonly DynamicCustomFunction s_textRawStringLiteral =
        CreateFunction(static (JsonElement json) => JsonSerializer.Serialize(json, s_jsonOptionsRawStringLiteral));
    private static readonly DynamicCustomFunction s_textVerbatimStringLiteral =
        CreateFunction(static (JsonElement json) => StringFunctions.Literal(JsonSerializer.Serialize(json)));

    public TemplateScriptObject(
        LanguageFeatures languageFeatures,
        string accessModifier,
        string recordDeclaration)
    {
        SetValue("text", (languageFeatures & LanguageFeatures.RawStringLiterals) != 0 ? s_textRawStringLiteral : s_textVerbatimStringLiteral, readOnly: true);
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
