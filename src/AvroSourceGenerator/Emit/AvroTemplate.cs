using System.Collections.Immutable;
using System.Text.Json;
using AvroSourceGenerator.Configuration;
using AvroSourceGenerator.Parsing;
using AvroSourceGenerator.Registry;
using AvroSourceGenerator.Schemas;
using Scriban;
using Scriban.Functions;
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

        var registeredSchemas = schemaRegistry.ToImmutableDictionary(x => x.SchemaName);

        return
        [
            .. schemaRegistry.Where(schema => schema.ShouldEmitCode).Select(schema =>
            {
                templateContext.SetValue(new ScriptVariableGlobal("Schema"), schema);
                templateContext.SetValue(new ScriptVariableGlobal("SchemaJson"), GetSchemaJson(schema, registeredSchemas, settings));
                var hintName = $"{schema.SchemaName.FullName}.Avro.g.cs";
                var sourceText = template.Render(templateContext);
                return new RenderedSchema(hintName, sourceText);
            })
        ];
    }

    private static string GetSchemaJson(TopLevelSchema schema, ImmutableDictionary<SchemaName, TopLevelSchema> registeredSchemas, RenderSettings settings)
    {
        return (settings.LanguageFeatures & LanguageFeatures.RawStringLiterals) != 0
            ? string.Join("\n", "\"\"\"", schema.ToJsonString(registeredSchemas, new JsonWriterOptions { Indented = true }), "\"\"\"")
            : StringFunctions.Literal(schema.ToJsonString(registeredSchemas));
    }
}
