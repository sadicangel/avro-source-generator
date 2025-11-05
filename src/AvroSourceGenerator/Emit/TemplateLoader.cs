using System.Reflection;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

namespace AvroSourceGenerator.Emit;

internal sealed class TemplateLoader : ITemplateLoader
{
    private static readonly Dictionary<string, string> s_templatePaths;

    private static readonly Dictionary<string, Template> s_templates;

    static TemplateLoader()
    {
        s_templatePaths = new Dictionary<string, string>
        {
            ["abstract"] = "AvroSourceGenerator.Templates.abstract.sbncs",
            ["enum"] = "AvroSourceGenerator.Templates.enum.sbncs",
            ["error"] = "AvroSourceGenerator.Templates.error.sbncs",
            ["field"] = "AvroSourceGenerator.Templates.field.sbncs",
            ["fixed"] = "AvroSourceGenerator.Templates.fixed.sbncs",
            ["getput"] = "AvroSourceGenerator.Templates.getput.sbncs",
            ["protocol"] = "AvroSourceGenerator.Templates.protocol.sbncs",
            ["record"] = "AvroSourceGenerator.Templates.record.sbncs",
            ["schema"] = "AvroSourceGenerator.Templates.schema.sbncs",
        };

        s_templates = new Dictionary<string, Template>(s_templatePaths.Count);
        foreach (var templatePath in s_templatePaths.Values)
        {
            s_templates[templatePath] = LoadTemplate(templatePath);
        }

        return;

        static Template LoadTemplate(string templatePath)
        {
            var assembly = Assembly.GetExecutingAssembly().GetManifestResourceStream(templatePath)
                ?? throw new InvalidOperationException();

            using var reader = new StreamReader(assembly);
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
