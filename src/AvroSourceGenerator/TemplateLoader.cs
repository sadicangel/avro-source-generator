using System.Reflection;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

namespace AvroSourceGenerator;

internal sealed class TemplateLoader : ITemplateLoader
{
    internal static readonly Dictionary<string, string> TemplatePaths = new()
    {
        ["aliases"] = "AvroSourceGenerator.Templates.aliases.sbncs",
        ["comment"] = "AvroSourceGenerator.Templates.comment.sbncs",
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
        TemplatePaths[templateName];

    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        using var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(templatePath));
        return reader.ReadToEnd();
    }

    public async ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath) =>
        await Task.FromResult(Load(context, callerSpan, templatePath));
}
