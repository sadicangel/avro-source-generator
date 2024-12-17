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

        foreach (var entry in TemplateLoader.Templates)
        {
            templateContext.CachedTemplates[entry.Key] = entry.Value;
        }

        var template = TemplateLoader.GetTemplate("schema");
        foreach (var schema in schemaRegistry)
        {
            templateContext.SetValue(new ScriptVariableGlobal("Schema"), schema);
            var safeName = schema.Name[0] is '@' ? schema.Name[1..] : schema.Name;
            var hintName = $"{safeName}.Avro.g.cs";
            var sourceText = template.Render(templateContext);
            yield return new RenderOutput(hintName, sourceText);
        }
    }
}

file sealed class TemplateLoader : ITemplateLoader
{
    private static readonly Dictionary<string, string> s_templatePaths;

    private static readonly Dictionary<string, Template> s_templates;

    static TemplateLoader()
    {
        s_templatePaths = new()
        {
            ["enum"] = "AvroSourceGenerator.Templates.enum.sbncs",
            ["error"] = "AvroSourceGenerator.Templates.error.sbncs",
            ["field"] = "AvroSourceGenerator.Templates.field.sbncs",
            ["fixed"] = "AvroSourceGenerator.Templates.fixed.sbncs",
            ["getput"] = "AvroSourceGenerator.Templates.getput.sbncs",
            ["record"] = "AvroSourceGenerator.Templates.record.sbncs",
            ["schema"] = "AvroSourceGenerator.Templates.schema.sbncs",
        };

        s_templates = new(s_templatePaths.Count);
        foreach (var templatePath in s_templatePaths.Values)
        {
            s_templates[templatePath] = LoadTemplate(templatePath);
        }

        static Template LoadTemplate(string templatePath)
        {
            using var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(templatePath));
            return Template.Parse(reader.ReadToEnd(), templatePath);
        }
    }

    public static IReadOnlyDictionary<string, Template> Templates => s_templates;

    public static Template GetTemplate(string templateName) =>
        s_templates[s_templatePaths[templateName]];

    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName) =>
        s_templatePaths[templateName];

    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath) =>
        throw new InvalidOperationException("This method should not be called.");
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
