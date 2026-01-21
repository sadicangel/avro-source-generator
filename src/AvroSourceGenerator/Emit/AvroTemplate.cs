using System.Collections.Immutable;
using AvroSourceGenerator.Parsing;
using AvroSourceGenerator.Registry;
using Scriban;
using Scriban.Syntax;

namespace AvroSourceGenerator.Emit;

internal static class AvroTemplate
{
    public static ImmutableArray<RenderedSchema> Render(SchemaRegistry schemaRegistry, RenderSettings settings)
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

        return
        [
            .. schemaRegistry.Where(schema => schema.ShouldEmitCode).Select(schema =>
            {
                templateContext.SetValue(new ScriptVariableGlobal("Schema"), schema);
                var hintName = $"{schema.SchemaName.FullName}.Avro.g.cs";
                var sourceText = template.Render(templateContext);
                return new RenderedSchema(hintName, sourceText);
            })
        ];
    }
}
