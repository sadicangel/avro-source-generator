using System.Reflection;
using AvroSourceGenerator.Schemas;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using Scriban.Syntax;

namespace AvroSourceGenerator;

internal readonly record struct RenderOutput(string HintName, string SourceText);

internal static class AvroTemplate
{
    public static IEnumerable<RenderOutput> Render(SchemaRegistry schemaRegistry, LanguageFeatures languageFeatures, string recordDeclaration, string accessModifier)
    {
        var templateContext = new TemplateContext()
        {
            MemberRenamer = member => member.Name,
            TemplateLoader = new TemplateLoader(),
        };

        var schemaTemplatePath = templateContext.TemplateLoader.GetPath(templateContext, default, "schema");
        var schemaTemplate = templateContext.CachedTemplates[schemaTemplatePath] = Template.Parse(templateContext.TemplateLoader.Load(templateContext, default, schemaTemplatePath));

        // TODO: Can we implement IScriptObject to represent the scope?
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
        templateContext.MemberRenamer);

        templateContext.PushGlobal(scope);
        foreach (var schema in schemaRegistry)
        {
            templateContext.SetValue(new ScriptVariableGlobal("Schema"), schema);
            var hintName = $"{schema.Name}.Avro.g.cs";
            var sourceText = schemaTemplate.Render(templateContext);
            yield return new RenderOutput(hintName, sourceText);
        }
        templateContext.PopGlobal();
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
