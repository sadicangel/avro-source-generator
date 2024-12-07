using System.Reflection;
using AvroSourceGenerator.Schemas;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

namespace AvroSourceGenerator;

internal static class AvroTemplate
{
    // Base context for all rendering.
    private static readonly TemplateContext s_templateContext = new()
    {
        MemberRenamer = member => member.Name,
        TemplateLoader = new TemplateLoader(),
    };

    internal static Template MainTemplate
    {
        get
        {
            var path = s_templateContext.TemplateLoader.GetPath(s_templateContext, default, "main");
            if (!s_templateContext.CachedTemplates.TryGetValue(path, out var template))
                template = s_templateContext.CachedTemplates[path] = Template.Parse(s_templateContext.TemplateLoader.Load(s_templateContext, default, path));
            return template;
        }
    }

    public static string Render(SchemaRegistry schemaRegistry, LanguageFeatures languageFeatures, string recordDeclaration, string accessModifier)
    {
        var scope = new ScriptObject();
        scope.Import(new
        {
            SchemaRegistry = schemaRegistry,
            RecordDeclaration = recordDeclaration,
            AccessModifier = accessModifier,
            UseNullableReferenceTypes = (languageFeatures & LanguageFeatures.NullableReferenceTypes) != 0,
            UseRequiredProperties = (languageFeatures & LanguageFeatures.RequiredProperties) != 0,
            UseInitOnlyProperties = (languageFeatures & LanguageFeatures.InitOnlyProperties) != 0,
            UseUnsafeAccessors = (languageFeatures & LanguageFeatures.UnsafeAccessors) != 0,
        },
        filter: null,
        s_templateContext.MemberRenamer);

        s_templateContext.PushGlobal(scope);
        var output = MainTemplate.Render(s_templateContext);
        s_templateContext.PopGlobal();

        return output;
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

    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName) =>
        s_templatePaths[templateName];

    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        using var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(templatePath));
        return reader.ReadToEnd();
    }

    public async ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath) =>
        await Task.FromResult(Load(context, callerSpan, templatePath));
}
