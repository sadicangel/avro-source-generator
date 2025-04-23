using AvroSourceGenerator.Schemas;
using Scriban;
using Scriban.Syntax;

namespace AvroSourceGenerator.Emit;

internal static class AvroTemplate
{
    public static IEnumerable<RenderOutput> Render(
        SchemaRegistry schemaRegistry,
        LanguageFeatures languageFeatures,
        string accessModifier,
        string recordDeclaration)
    {
        var templateContext = new TemplateContext(new TemplateScriptObject(languageFeatures, accessModifier, recordDeclaration))
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
            var hintName = $"{schema.SchemaName.FullName}.Avro.g.cs";
            var sourceText = template.Render(templateContext);
            yield return new RenderOutput(hintName, sourceText);
        }
    }
}
