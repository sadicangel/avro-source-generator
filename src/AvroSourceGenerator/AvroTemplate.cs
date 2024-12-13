using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using AvroSourceGenerator.Schemas;
using Scriban;
using Scriban.Functions;
using Scriban.Parsing;
using Scriban.Runtime;
using Scriban.Syntax;

namespace AvroSourceGenerator;

internal readonly record struct RenderOutput(string HintName, string SourceText);

internal static class AvroTemplate
{
    public static IEnumerable<RenderOutput> Render(SchemaRegistry schemaRegistry, LanguageFeatures languageFeatures, string recordDeclaration, string accessModifier)
    {
        var templateContext = new TemplateContext(new TemplateScriptObject(languageFeatures, recordDeclaration, accessModifier))
        {
            MemberRenamer = member => member.Name,
            TemplateLoader = new TemplateLoader(),
        };

        var template = GetMainTemplate(templateContext);
        foreach (var schema in schemaRegistry)
        {
            templateContext.SetValue(new ScriptVariableGlobal("Schema"), schema);
            var hintName = $"{schema.Name.Replace("@", "")}.Avro.g.cs";
            var sourceText = template.Render(templateContext);
            yield return new RenderOutput(hintName, sourceText);
        }
    }

    private static Template GetMainTemplate(TemplateContext context)
    {
        const string MainTemplateName = "schema";
        var templatePath = context.TemplateLoader.GetPath(context, default, MainTemplateName);
        // TODO: Maybe avoid parsing the template each time.
        var template = Template.Parse(context.TemplateLoader.Load(context, default, templatePath));
        return template;
    }
}

file sealed class TemplateLoader : ITemplateLoader
{
    private static readonly Dictionary<string, string> s_templatePaths = new()
    {
        ["enum"] = "AvroSourceGenerator.Templates.enum.sbncs",
        ["error"] = "AvroSourceGenerator.Templates.error.sbncs",
        ["field"] = "AvroSourceGenerator.Templates.field.sbncs",
        ["fixed"] = "AvroSourceGenerator.Templates.fixed.sbncs",
        ["getput"] = "AvroSourceGenerator.Templates.getput.sbncs",
        ["main"] = "AvroSourceGenerator.Templates.main.sbncs",
        ["record"] = "AvroSourceGenerator.Templates.record.sbncs",
        ["schema"] = "AvroSourceGenerator.Templates.schema.sbncs",
    };

    private static readonly ConcurrentDictionary<string, string> s_templates = new();

    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName) =>
        s_templatePaths[templateName];

    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        return s_templates.GetOrAdd(templatePath, LoadTemplate);

        static string LoadTemplate(string templatePath)
        {
            using var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(templatePath));
            return reader.ReadToEnd();
        }
    }
}

file sealed class TemplateScriptObject : BuiltinFunctions
{
    private static readonly DynamicCustomFunction s_text_RawStringLiteral =
        CreateFunction(static (JsonElement json) => JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true }));
    private static readonly DynamicCustomFunction s_text_VerbatimStringLiteral =
        CreateFunction(static (JsonElement json) => StringFunctions.Literal(JsonSerializer.Serialize(json)));

    public TemplateScriptObject(
        LanguageFeatures languageFeatures,
        string recordDeclaration,
        string accessModifier)
    {
        SetValue("text", (languageFeatures & LanguageFeatures.RawStringLiterals) != 0 ? s_text_RawStringLiteral : s_text_VerbatimStringLiteral, readOnly: true);
        SetValue("RecordDeclaration", recordDeclaration, readOnly: true);
        SetValue("AccessModifier", accessModifier, readOnly: true);
        SetValue("UseNullableReferenceTypes", (languageFeatures & LanguageFeatures.NullableReferenceTypes) != 0, readOnly: true);
        SetValue("UseRequiredProperties", (languageFeatures & LanguageFeatures.RequiredProperties) != 0, readOnly: true);
        SetValue("UseInitOnlyProperties", (languageFeatures & LanguageFeatures.InitOnlyProperties) != 0, readOnly: true);
        SetValue("UseRawStringLiterals", (languageFeatures & LanguageFeatures.RawStringLiterals) != 0, readOnly: true);
        SetValue("UseUnsafeAccessors", (languageFeatures & LanguageFeatures.UnsafeAccessors) != 0, readOnly: true);
    }

    private static DynamicCustomFunction CreateFunction(Delegate @delegate) =>
        DynamicCustomFunction.Create(@delegate);
}
