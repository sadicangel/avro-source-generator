using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Parsing;
using AvroSourceGenerator.Registry;
using AvroSourceGenerator.Schemas;
using Scriban;
using Scriban.Functions;
using Scriban.Runtime;
using Scriban.Syntax;

namespace AvroSourceGenerator.Emit;

internal static class AvroTemplate
{
    public static ImmutableArray<RenderedSchema> Render(SchemaRegistry schemaRegistry, RenderSettings settings)
    {
        var templateContext = new TemplateContext(new TemplateScriptObject(settings, CreateJsonSchemaRender(schemaRegistry, settings)))
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

    private static DynamicCustomFunction? CreateJsonSchemaRender(SchemaRegistry schemaRegistry, RenderSettings settings)
    {
        if (settings.AvroLibrary != AvroLibrary.Apache)
        {
            return null;
        }

        var registeredSchemas = schemaRegistry.Schemas;

        return (settings.LanguageFeatures & LanguageFeatures.RawStringLiterals) != 0
            ? DynamicCustomFunction.Create((AvroSchema schema) => string.Join("\n", "\"\"\"", schema.ToJsonString(registeredSchemas, new JsonWriterOptions { Indented = true }), "\"\"\""))
            : DynamicCustomFunction.Create((AvroSchema schema) => StringFunctions.Literal(schema.ToJsonString(registeredSchemas)));
    }
}
