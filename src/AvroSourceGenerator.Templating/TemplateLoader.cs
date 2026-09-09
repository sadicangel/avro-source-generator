using System.Reflection;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

namespace AvroSourceGenerator.Templating;

internal sealed class TemplateLoader(RenderOptions options) : ITemplateLoader
{
    private static readonly Dictionary<string, string> s_templatePaths = BuildTemplatePaths();

    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName) =>
        templateName switch
        {
            "apache.put" when !options.UseInitOnlyProperties => s_templatePaths["apache.put_mutable"],
            "apache.put" when !options.UseUnsafeAccessors => s_templatePaths["apache.put_immutable_reflection"],
            "apache.put" => s_templatePaths["apache.put_immutable_unsafe"],
            "fixed" => s_templatePaths["apache.fixed"],
            _ => s_templatePaths[templateName],
        };

    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        var assembly = Assembly.GetExecutingAssembly().GetManifestResourceStream(templatePath)
            ?? throw new InvalidOperationException($"Template resource '{templatePath}' was not found.");

        using var reader = new StreamReader(assembly);
        return reader.ReadToEnd();
    }

    private static Dictionary<string, string> BuildTemplatePaths()
    {
        const string TemplateNamespace = "AvroSourceGenerator.Templating.Templates";
        const string TemplateExtension = ".sbncs";
        return Assembly.GetExecutingAssembly().GetManifestResourceNames()
            .Where(name => name.StartsWith(TemplateNamespace) && name.EndsWith(TemplateExtension))
            .ToDictionary(GetTemplateName);

        static string GetTemplateName(string templatePath) =>
            templatePath.AsSpan()[(TemplateNamespace.Length + 1)..^TemplateExtension.Length].ToString();
    }
}
