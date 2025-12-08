using AvroSourceGenerator.Parsing;
using AvroSourceGenerator.Registry;
using Scriban;
using Scriban.Syntax;

namespace AvroSourceGenerator.Emit;

internal static class AvroTemplate
{
    public static IEnumerable<RenderOutput> Render(SchemaRegistry schemaRegistry, RenderSettings settings)
    {
        var templateContext = new TemplateContext(new TemplateScriptObject(settings))
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
